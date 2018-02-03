using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using Crossover;

namespace Crossover
{
    class Parser
    {
        List<Token> scriptTokenList = new List<Token>();

        List<Variable> usableVariables = new List<Variable>();
        List<Function> usableFunctions = new List<Function>();

        //List of externally imported functions, and variables for those functions to use
        List<Variable> externalVariables = new List<Variable>();
        List<Function> externalFunctions = new List<Function>();

        //Number representing "depth", or what number nested loop we are in right now (e.g. blank program is 0, inside function is 1, inside if loop inside function is 2)
        int currentDepthLevel = 0;

        bool waitingForFunctionStart = false;
        bool waitingForFunctionEnd = false;

        //Make true if parentheses are detected (which signal the start of parameters)
        bool inParams = false;

        public void ActOnTokens(List<Token> tokenList, bool isInFunction, bool isInExternalScript)
        {
            scriptTokenList = tokenList;

            //For each token in tokenList
            for (int i = 0; i < tokenList.Count; i++)
            {
                //Check TokenType
                switch (tokenList[i].type)
                {
                    //If token is a variable declaration ('var')
                    case TokenType.VariableDeclaration:
#region
                        //Temporary: no local in-function variables can be declared
                        if (!isInFunction)
                        {
                            Variable newVariable = new Variable();

                            //If the following token is not an identifier, then throw an error
                            if (tokenList[i + 1].type != TokenType.Identifier)
                                CrossoverCompiler.ThrowCompilerError("Variable must be named immediately after it is declared", tokenList[i].isOnLine);
                            else
                            {
                                //If the previous token is the 'exclusive' keyword AND this is not the first token
                                if (i > 0 && tokenList[i - 1].type == TokenType.ExclusiveKeyword)
                                    newVariable.isExclusive = true;

                                //If the following token is an identifier, use it as the new variable's name
                                newVariable.name = tokenList[i + 1].value;

                                //Parent script of the variable is the current script's file name
                                newVariable.parentScript = "this";

                                //Depth level of the variable is the current depth level of the script
                                newVariable.depthLevel = currentDepthLevel;

                                //This variable is a usable variable that was declared in this script
                                usableVariables.Add(newVariable);

                                //Skip the next iteration
                                i += 1;
                            }
                            break;
                        }
                        break;
#endregion
                    //If token is a function declaration ('function')
                    case TokenType.FunctionDeclaration:
#region
                        //If we are not already in a function
                        if (!isInFunction)
                        {
                            Function newFunction = new Function();

                            //We are now awaiting an opening curly brace
                            waitingForFunctionStart = true;

                            //If the following token is not an identifier, then throw an error
                            if (tokenList[i + 1].type != TokenType.Identifier)
                                CrossoverCompiler.ThrowCompilerError("Function must be named immediately after it is declared", tokenList[i].isOnLine);
                            else
                            {
                                if (i > 0 && tokenList[i - 1].type == TokenType.ExclusiveKeyword)
                                    newFunction.scope = AccessLevel.Exclusive;

                                //If the following token is an identifier, use it as the new variable's name and skip the next iteration
                                newFunction.name = tokenList[i + 1].value;
                                usableFunctions.Add(newFunction);

                                i += 1;
                            }

                            break;
                        }
                        break;
                        
                    #endregion
                    //If token is the 'print' keyword
                    case TokenType.PrintKeyword:
#region
                        Token followingToken = tokenList[i+1];

                        //Check if following token is direct string, bool, int, or float
                        if (followingToken.type == TokenType.StringVariable || followingToken.type == TokenType.BoolVariable || followingToken.type == TokenType.IntVariable || followingToken.type == TokenType.FloatVariable)
                        {
                            //Write the value of the following token to the console, skip the next iteration
                            Console.WriteLine(followingToken.value);
                            i += 1;
                        }

                        //Else if it's an identifier, check if it matches any variable
                        else if (followingToken.type == TokenType.Identifier)
                        {
                            //Write the value of the variable to the console, skip the next iteration
                            Console.WriteLine(GetVariableByName(followingToken.value, followingToken.isOnLine, isInExternalScript).value);
                            i += 1;
                        }

                        break;
#endregion
                    //If token is the 'use' keyword
                    case TokenType.UseKeyword:
#region
                        if (!isInExternalScript)
                        {
                            Token pathOfExternalFile = tokenList[i + 1];

                            //If this is being executed inside a function, throw an error
                            if (isInFunction)
                                CrossoverCompiler.ThrowCompilerError("Cannot use 'use' keyword inside of a function", tokenList[i].isOnLine);

                            if (pathOfExternalFile.type != TokenType.StringVariable)
                                CrossoverCompiler.ThrowCompilerError("A string with the path of the imported file must follow the 'use' keyword", tokenList[i].isOnLine);

                            Tokenizer externalInputTokenizer = new Tokenizer();

                            //Read file from path, get a list of tokens, filter through the list to find functions, add functions to externalFunctions
                            FindFunctionsAndVariablesInExternal(externalInputTokenizer.InputToTokensList(File.ReadAllText(@pathOfExternalFile.value)), pathOfExternalFile.value);

                            break;
                        }

                        else
                        {
                            CrossoverCompiler.ThrowCompilerError("Cannot access other external scripts from an external script", tokenList[i].isOnLine);
                        }

                        break;
#endregion
                    //If token is the 'as' keyword
                    case TokenType.AsKeyword:
#region
                        //If this token comes after a 'use' keyword (not directly after)
                        if (tokenList[i-2].type == TokenType.UseKeyword)
                        {
                            //Get the previous token and use it as the filename to check for
                            Token fileNameToCheckFor = tokenList[i - 1];

                            //Get the next token and use it as the name that will represent the imported module
                            Token nameToUseAs = tokenList[i + 1];

                            //Skip next iteration
                            i++;

                            //If this is being executed inside a function, throw an error
                            if (isInFunction)
                                CrossoverCompiler.ThrowCompilerError("Cannot use 'as' keyword inside of a function", tokenList[i].isOnLine);

                            //If the next token is not an identifier, throw an error
                            if (nameToUseAs.type != TokenType.Identifier)
                                CrossoverCompiler.ThrowCompilerError("An identifier with the desired name to use for the imported module must follow the 'as' keyword", tokenList[i].isOnLine);

                            //Foreach currently registered external variable
                            foreach (Variable var in externalVariables)
                            {
                                //If the current iteration's variable matches the file name that we have to check for
                                if (var.parentScript == fileNameToCheckFor.value)
                                {
                                    //Set the parent script of that variable to the name that we want it to be accessible by
                                    var.parentScript = nameToUseAs.value;
                                }
                            }

                            //Foreach currently registered external function
                            foreach (Function func in externalFunctions)
                            {
                                //If the current iteration's variable matches the file name that we have to check for
                                if (func.parentScript == fileNameToCheckFor.value)
                                {
                                    //Set the parent script of that variable to the name that we want it to be accessible by
                                    func.parentScript = nameToUseAs.value;
                                }
                            }

                            break;
                        }

                        else
                        {
                            CrossoverCompiler.ThrowCompilerError("The 'as' keyword must be used in conjunction with the 'use' keyword", tokenList[i].isOnLine);
                        }

                        break;
#endregion
                    //If token is the 'external' keyword
                    case TokenType.ExternalKeyword:
#region
                        if (!isInExternalScript)
                        {
                            Token nameOfExternalScript = tokenList[i + 2];

                            Token externalVariableOrFunctionToFind = tokenList[i + 4];

                            bool canFindExternalScript = false;

                            bool canFindExternalVariableOrFunction = false;

                            //Syntax check
                            //If the following tokens are not period, identifier, period, identifier, then throw an error
                            if (tokenList[i + 1].type != TokenType.Period || tokenList[i + 2].type != TokenType.Identifier && tokenList[i + 3].type != TokenType.Period && tokenList[i + 4].type != TokenType.Identifier)
                                CrossoverCompiler.ThrowCompilerError("'external' keyword must be followed by a period, then identifier, then another period, then another identifier", tokenList[i].isOnLine);

                            //RELOCATE to equals check
                            //Foreach external variable that has been imported from external scripts
                            foreach (Variable var in externalVariables)
                            {
                                //If the function's parent script match the one specified in the identifier
                                if (var.parentScript == nameOfExternalScript.value)
                                {
                                    canFindExternalScript = true;

                                    //If the token after the second period matches any of the external functions
                                    if (externalVariableOrFunctionToFind.value == var.name)
                                    {
                                        canFindExternalVariableOrFunction = true;
                                    }
                                }
                            }

                            //Foreach external function that has been imported from external scripts
                            foreach (Function func in externalFunctions)
                            {
                                //If the function's parent script match the one specified in the identifier
                                if (func.parentScript == nameOfExternalScript.value)
                                {
                                    canFindExternalScript = true;

                                    //If the token after the second period matches any of the external functions
                                    if (externalVariableOrFunctionToFind.value == func.name)
                                    {
                                        //Do the function that it matches
                                        canFindExternalVariableOrFunction = true;
                                        RunFunction(func, true);
                                    }
                                }
                            }

                            //If external script cannot be found
                            if (!canFindExternalScript)
                            {
                                //Throw error
                                CrossoverCompiler.ThrowCompilerError("Could not find external script imported as: " + nameOfExternalScript.value, tokenList[i].isOnLine);
                            }

                            //If external function cannot be found
                            if (!canFindExternalVariableOrFunction)
                            {
                                //Throw error
                                CrossoverCompiler.ThrowCompilerError("Could not find external function with the name: " + externalVariableOrFunctionToFind.value, tokenList[i].isOnLine);
                            }

                            i += 4;

                            break;
                        }
                        else
                        {
                            CrossoverCompiler.ThrowCompilerError("Cannot access other external scripts from an external script", tokenList[i].isOnLine);
                        }
                        break;
                    #endregion
                    //If token is an identifier
                    case TokenType.Identifier:
#region

                        Token currentIdentifier = tokenList[i];

                        bool foundVariableOrFunction = false;

                        //Foreach locally accessible function (external function checks are handled in the 'external' token case
                        foreach (Function func in usableFunctions)
                        {
                            //If the name of the function currently iterated equals the value of the identifier (the current token)
                            if (func.name == currentIdentifier.value)
                            {
                                foundVariableOrFunction = true;

                                //If the function takes in parameters
                                if (func.parameters.Count > 0)
                                {
                                    //Handle the function's parameters, set the current iteration to whatever it is after the parameters are evaluated
                                    i = HandleParameters(i, func);
                                }

                                //If the function doesn't take in parameters, run the function
                                else
                                {
                                    RunFunction(func, false);
                                }
                            }
                        }

                        //If no function of the name of the identifier was found
                        if (!foundVariableOrFunction)
                        {
                            //Foreach usable variable
                            foreach (Variable var in usableVariables)
                            {
                                //If the currently iterated variable's name equals the value of the current identifier, we have found a VariableOrFunction
                                if (var.name == currentIdentifier.value)
                                    foundVariableOrFunction = true;
                            }
                        }

                        //If we haven't found a variable or function with a matching name, throw error
                        if (!foundVariableOrFunction)
                            CrossoverCompiler.ThrowCompilerError("Could not find local variable or function with name: " + currentIdentifier.value, tokenList[i].isOnLine);

                        break;
#endregion
                    //If token is a curly brace
                    case TokenType.CurlyBrace:
#region
                        switch (tokenList[i].value)
                        {
                            //If it is an opening brace (e.g. beginning of function)
                            case "{":
                                int startingDepthLevel = currentDepthLevel;

                                //Go deeper by one
                                currentDepthLevel += 1;

                                //Function contents have started
                                waitingForFunctionStart = false;
                                waitingForFunctionEnd = true;

                                //Go to next token
                                i += 1;

                                //While the function is still open and has not been closed with a brace yet
                                while (currentDepthLevel != startingDepthLevel)
                                {
                                    //If the token is an opening brace, add one to the depth level
                                    if (tokenList[i].value == "{")
                                        currentDepthLevel += 1;

                                    //If it's a closing brace, subtract one from the depth level
                                    else if (tokenList[i].value == "}")
                                    {
                                        currentDepthLevel -= 1;

                                        //If the brace closes the function, break before it is added to the function's contents
                                        if (currentDepthLevel == startingDepthLevel)
                                            break;
                                    }

                                    //Add token to the most recent function in the usable functions array
                                    usableFunctions[usableFunctions.Count - 1].contents.Add(tokenList[i]);

                                    //Go to next token
                                    i++;
                                }

                                break;

                            //If it is a closing brace
                            case "}":
                                //Go shallower by one
                                currentDepthLevel -= 1;

                                break;

                            default:
                                break;
                        }

                        break;
                    #endregion
                    //If token is a string, bool, int, or float variable VALUE, not NAME
                    case TokenType.StringVariable:
                    case TokenType.BoolVariable:
                    case TokenType.IntVariable:
                    case TokenType.FloatVariable:
#region
                        if (!isInExternalScript)
                        {
                            //If the previous token is an equals AND not the 'use' keyword AND we are NOT in parameters, if the token 2 iterations back is an identifier
                            if (tokenList[i-1].type == TokenType.Equals && tokenList[i - 1].type != TokenType.UseKeyword && !inParams)
                            {
                                if (tokenList[i-2].type == TokenType.Identifier)
                                {
                                    //Get the variable with the identifier's name, and set its value equal to this token's value
                                    GetVariableByName(tokenList[i - 2].value, tokenList[i - 2].isOnLine, false).value = tokenList[i].value;
                                }

                                else
                                {
                                    CrossoverCompiler.ThrowCompilerError("Variable values must be used to set a valid variable or compare to another string value", tokenList[i].isOnLine);
                                }
                            }
                            
                            //Else if the previous token is an equals AND we ARE in parameters !!! EDIT: should handle in 'if' declaration

                            break;
                        }
                        //If is in external script
                        else
                        {
                            //If the previous token is an equals AND we are NOT in parameters, if the token 2 iterations back is an identifier
                            if (tokenList[i - 1].type == TokenType.Equals && !inParams)
                            {
                                if (tokenList[i - 2].type == TokenType.Identifier)
                                {
                                    //Get the variable with the identifier's name, and set its value equal to this token's value
                                    GetVariableByName(tokenList[i - 2].value, tokenList[i - 2].isOnLine, true).value = tokenList[i].value;
                                }

                                else
                                {
                                    CrossoverCompiler.ThrowCompilerError("Variable values must be used to set a valid variable or compare to another string value", tokenList[i].isOnLine);
                                }
                            }
                        }

                        break;
#endregion

                    //Default exit
                    default:
                        break;
                }
            }
        }

        int HandleParameters(int currentIteration, Function funcToHandle)
        {
            List<Token> detectedTokensWithinParentheses = new List<Token>();
            List<PassedParameter> detectedParametersWithinParentheses = new List<PassedParameter>();

            //Depth level of parentheses within parameters
            int parenthesisDepthLevel = 0;

            //If the next token is not an opening parenthesis, throw an error
            if (scriptTokenList[currentIteration + 1].type != TokenType.Parenthesis && scriptTokenList[currentIteration + 1].value.ToCharArray()[0] != '(')
                CrossoverCompiler.ThrowCompilerError("Function " + funcToHandle.name + " takes in parameters, and none were specified.", scriptTokenList[currentIteration].isOnLine);

            //Go to the token following the opening parenthesis
            currentIteration += 2;

            //Add one to the depth level because we've just had our opening parenthesis
            parenthesisDepthLevel += 1;

            //
            //  GET TOKENS
            //

            //Loop through all tokens until the original parameter block of parentheses is closed
            while (parenthesisDepthLevel != 0)
            {
                //If the token is an opening parenthesis, add one to the depth level
                if (scriptTokenList[currentIteration].type == TokenType.Parenthesis && scriptTokenList[currentIteration].value.ToCharArray()[0] != '(')
                    parenthesisDepthLevel += 1;

                //If the token is a closing parenthesis, subtract one from the depth level
                else if (scriptTokenList[currentIteration].type == TokenType.Parenthesis && scriptTokenList[currentIteration].value.ToCharArray()[0] != ')' && parenthesisDepthLevel == 1)
                    parenthesisDepthLevel -= 1;

                //Otherwise, add the token to the list of tokens within the parentheses and move to the next token
                else
                {
                    detectedTokensWithinParentheses.Add(scriptTokenList[currentIteration]);
                    currentIteration++;
                }
            }
            
            //
            //  GET PARAMETERS
            //

            PassedParameter currentParam = new PassedParameter();

            //Foreach token in all the tokens that were detected within the parentheses of the parameters
            foreach(Token token in detectedTokensWithinParentheses)
            {
                //If the token is a comma
                if (token.type == TokenType.Comma)
                {
                    //Add whatever the value of the parameter was to the detected parameters, reset the current parameter
                    detectedParametersWithinParentheses.Add(currentParam);
                    currentParam.contents.Clear();
                }

                //If the token is anything but a comma
                else
                {
                    //Add it to the contents of the current parameter
                    currentParam.contents.Add(token);
                }
            }

            //If the amount of parameters passed doesn't equal the amount of parameters that the function takes in, throw an error
            if (detectedParametersWithinParentheses.Count != funcToHandle.parameters.Count)
                CrossoverCompiler.ThrowCompilerError("Function " + funcToHandle.name + " takes in " + funcToHandle.parameters.Count + " parameters, but only " + detectedParametersWithinParentheses.Count + " were passed when the function was called.", scriptTokenList[currentIteration].isOnLine);

            //
            //  EVALUATE PARAMETERS
            //

            //Foreach parameter detected, try to evaluate it
            for (int i = 0; i < detectedParametersWithinParentheses.Count; i++)
            {
                PassedParameter param = detectedParametersWithinParentheses[i];

                //If the parameter contains more than one token, try to evaluate it into a valid value to pass to the function
                if (param.contents.Count > 1)
                {
                    //Try to evaluate the tokens into a single value
                    TryEvaluate(param.contents);

                    //PASS THAT VALUE TO THE FUNCTION
                }

                //If the parameter is only one token
                else
                {
                    //Local variable storing the value of the current parameter
                    Token paramValue = param.contents[0];

                    //If the type of the current parameter doesn't equal the type of the corresponding parameter's definition, throw an error
                    if (paramValue.type != funcToHandle.parameters[i].parameterVariable.type)
                        CrossoverCompiler.ThrowCompilerError(string.Format("Parameter {0} of function {1} is not of type {2}", funcToHandle.parameters[i].parameterVariable.name, funcToHandle.name, funcToHandle.parameters[i].parameterVariable.type), scriptTokenList[currentIteration].isOnLine);

                    //If the Token is an identifier, set the value of the parameter in the function to the value of the variable passed
                    funcToHandle.parameters[i].parameterVariable = GetVariableByName(paramValue.value, paramValue.isOnLine, false);

                    //If the Token is a RAW VALUE, set the value of the parameter in the function to the raw value passed
                }
            }

            //Return the updated iteration number
            return currentIteration;
        }

        //Use to evaluate expressions involving operators
        Variable TryEvaluate(List<Token> tokensToEvaluate)
        {
            Variable varToReturn = new Variable();

            Token firstToken = new Token();
            Token operatorToUse = new Token();

            DataTable dtTool = new DataTable();

            //For every token in the tokens that need to be evaluated
            for (int i = 0; i < tokensToEvaluate.Count; i++)
            {
                Token currentToken = tokensToEvaluate[i];

                //Error check
                //If the token is NOT of an operable type, throw an error
                if (currentToken.type != TokenType.IntVariable || currentToken.type != TokenType.FloatVariable || currentToken.type != TokenType.StringVariable || currentToken.type != TokenType.MathematicalOperator)
                    CrossoverCompiler.ThrowCompilerError("Cannot operate on this data type.", currentToken.isOnLine);
                if (currentToken.type == TokenType.MathematicalOperator)
                    if (i == 0)
                        CrossoverCompiler.ThrowCompilerError("The first value in an expression must be an operable data type, not an operator.", currentToken.isOnLine);


                //If it's the first token that we need to evaluate, get its type
                if (i == 0)
                    firstToken.type = currentToken.type;

                //If the token is an operator, set it as the operator to use
                else if (currentToken.type == TokenType.MathematicalOperator)
                    operatorToUse.value = currentToken.value;

                //If it's not the first token and not an operator
                else
                {
                    //If the tokens are valid to operate on
                    if (CheckOperable(firstToken, currentToken))
                    {
                        //Switch through the possible types of the first token
                        switch (firstToken.type)
                        {
                            //If the first token is an int or a float
                            case TokenType.IntVariable:
                            case TokenType.FloatVariable:
                                //Compute result of expression
                                varToReturn.value = dtTool.Compute(firstToken.value + operatorToUse.value + currentToken.value, "").ToString();

                                break;

                            case TokenType.StringVariable:
                                //If the operater that we're going to use is not a plus, throw an error
                                if (operatorToUse.value != '+'.ToString())
                                    CrossoverCompiler.ThrowCompilerError("Strings can only be added to.", currentToken.isOnLine);

                                varToReturn.value = firstToken.value + currentToken.value;

                                break;

                            default:
                                break;
                        }
                    }

                    //If the tokens are invalid to operate on, throw an error
                    else
                        CrossoverCompiler.ThrowCompilerError(string.Format("Cannot operate on types of {0} and {1}.", firstToken.type.ToString(), currentToken.type.ToString()), currentToken.isOnLine);
                }
            }

            //Return value of evaluation if can evaluate
            //Return null if cannot evaluate
            return varToReturn;
        }

        //Checks if two tokens can be operated on
        bool CheckOperable(Token firstToken, Token secondToken)
        {
            //Error check
            //If either of the tokens are bools
            if (firstToken.type == TokenType.BoolVariable || secondToken.type == TokenType.BoolVariable)
                CrossoverCompiler.ThrowCompilerError("Cannot operate on boolean values.", firstToken.isOnLine);

            //If the tokens are the same type, return true because they can be operated on
            if (firstToken.type == secondToken.type)
                return true;

            //If the first token is an int and the second one is a float, return true because they can be operated on
            else if (firstToken.type == TokenType.IntVariable && secondToken.type == TokenType.FloatVariable)
                return true;

            //If the first token is a float and the second one is an int, return true because they can be operated on
            else if (firstToken.type == TokenType.FloatVariable && secondToken.type == TokenType.IntVariable)
                return true;

            //If none of these are triggered, return false
            else
                return false;
        }

        //Use to execute a function
        public void RunFunction(Function functionToRun, bool functionIsExternal)
        {
            ActOnTokens(functionToRun.contents, true, functionIsExternal);
        }

        //Use to find functions/variables in external file
        public void FindFunctionsAndVariablesInExternal(List<Token> tokensToCheck, string fileName)
        {
            int externalCurrentDepthLevel = 0;

            //For each token in tokensToCheck
            for (int i = 0; i < tokensToCheck.Count; i++)
            {
                //Check TokenType
                switch (tokensToCheck[i].type)
                {
                    //If token is a variable declaration ('var')
                    case TokenType.VariableDeclaration:
#region
                        Variable newVariable = new Variable();

                        //If the following token is not an identifier, then throw an error
                        if (tokensToCheck[i + 1].type != TokenType.Identifier)
                            CrossoverCompiler.ThrowCompilerError("Variable must be named immediately after it is declared", tokensToCheck[i].isOnLine);
                        else
                        {
                            //If the previous token is the 'exclusive' keyword AND this is not the first token
                            if (i > 0 && tokensToCheck[i - 1].type == TokenType.ExclusiveKeyword)
                                newVariable.isExclusive = true;

                            //If the following token is an identifier, use it as the new variable's name and skip the next iteration
                            newVariable.name = tokensToCheck[i + 1].value;
                            newVariable.depthLevel = currentDepthLevel;

                            //Set parent script to the file that the variable comes from
                            newVariable.parentScript = fileName;

                            externalVariables.Add(newVariable);

                            i += 1;
                        }
                        break;
#endregion
                    //If token is a function declaration ('function')
                    case TokenType.FunctionDeclaration:
#region
                        Function newFunction = new Function();

                        //We are now awaiting an opening curly brace
                        waitingForFunctionStart = true;

                        //If the following token is not an identifier, then throw an error
                        if (tokensToCheck[i + 1].type != TokenType.Identifier)
                            CrossoverCompiler.ThrowCompilerError("Function must be named immediately after it is declared", tokensToCheck[i].isOnLine);
                        else
                        {
                            if (i > 0 && tokensToCheck[i - 1].type == TokenType.ExclusiveKeyword)
                                newFunction.scope = AccessLevel.Exclusive;

                            //If the function isn't exclusive
                            if (newFunction.scope != AccessLevel.Exclusive)
                            {
                                //Use identifier as the new variable's name and skip the next iteration
                                newFunction.name = tokensToCheck[i + 1].value;

                                //Set parent script to the file that the function comes from
                                newFunction.parentScript = fileName;

                                externalFunctions.Add(newFunction);
                            }

                            //If it is, then break out
                            else
                            {
                                break;
                            }

                            i += 1;
                        }

                        break;
#endregion
                    //If token is a curly brace
                    case TokenType.CurlyBrace:
#region
                        switch (tokensToCheck[i].value)
                        {
                            //If it is an opening brace (e.g. beginning of function)
                            case "{":
                                int startingDepthLevel = externalCurrentDepthLevel;

                                //Go deeper by one
                                externalCurrentDepthLevel += 1;

                                //Function contents have started
                                waitingForFunctionStart = false;
                                waitingForFunctionEnd = true;

                                //Go to next token
                                i += 1;

                                //While the function is still open and has not been closed with a brace yet
                                while (externalCurrentDepthLevel != startingDepthLevel)
                                {
                                    //If the token is an opening brace, add one to the depth level
                                    if (tokensToCheck[i].value == "{")
                                        externalCurrentDepthLevel += 1;

                                    //If it's a closing brace, subtract one from the depth level
                                    else if (tokensToCheck[i].value == "}")
                                    {
                                        externalCurrentDepthLevel -= 1;

                                        //If the brace closes the function, break before it is added to the function's contents
                                        if (externalCurrentDepthLevel == startingDepthLevel)
                                            break;
                                    }

                                    //Add token to the most recent function in the external functions array
                                    externalFunctions[externalFunctions.Count - 1].contents.Add(tokensToCheck[i]);

                                    //Go to next token
                                    i++;
                                }

                                break;

                            //If it is a closing brace
                            case "}":
                                //Go shallower by one
                                externalCurrentDepthLevel -= 1;

                                break;

                            default:
                                break;
                        }
                        break;
                    #endregion
                    //If token is a string, bool, int, or float variable
                    case TokenType.StringVariable:
                    case TokenType.BoolVariable:
                    case TokenType.IntVariable:
                    case TokenType.FloatVariable:
#region
                        //If the previous token is an equals AND we are NOT in parameters, if the token 2 iterations back is an identifier
                        if (tokensToCheck[i - 1].type == TokenType.Equals && !inParams)
                        {
                            if (tokensToCheck[i - 2].type == TokenType.Identifier)
                            {
                                //Get the variable with the identifier's name, and set its value equal to this token's value
                                GetVariableByName(tokensToCheck[i - 2].value, tokensToCheck[i - 2].isOnLine, true).value = tokensToCheck[i].value;
                            }

                            else
                            {
                                CrossoverCompiler.ThrowCompilerError("String values must be used to set a valid variable or compare to another string value", tokensToCheck[i].isOnLine);
                            }
                        }

                        break;
#endregion
                    default:
                        break;
                }
            }

        }

        //Use to get a variable by name
        public Variable GetVariableByName(string varName, int onLine, bool inExternal)
        {
            //If this is not being executed in an externally-imported script
            if (!inExternal)
            {
                //For each variable in all the variables that have already been declared
                foreach (Variable var in usableVariables)
                {
                    //If the name of iteration's variable matches the name of the variable we are looking for, return that variable
                    if (var.name == varName)
                        return var;
                }
            }
            //If this is being executed in an externally-imported script
            else
            {
                //For each variable in all the variables that have already been declared
                foreach (Variable var in externalVariables)
                {
                    //If the name of iteration's variable matches the name of the variable we are looking for, return that variable
                    if (var.name == varName)
                        return var;
                }
            }

            //If none of the already declared variables match the one we're looking for, then throw an error
            CrossoverCompiler.ThrowCompilerError("No variable found with the name: " + varName, onLine);

            //TECHNICALLY UNREACHABLE, ThrowCompilerError() stops execution
            return null;
        }
    }
}