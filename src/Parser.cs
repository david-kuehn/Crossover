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

        //Indicates whether or not we are currently parsing through a function
        bool isCurrentlyExecutingFunction = false;

        //If a function is being executed, this will hold that function
        Function functionBeingExecutedByParser = new Function();

        public void ActOnTokens(List<Token> tokenList, bool isInFunction, Function funcBeingExecuted, bool isInExternalScript)
        {
            //If this is a function, set the script's variable to true and give it the function, if not, set it to false 
            if (isInFunction)
            {
                isCurrentlyExecutingFunction = true;
                functionBeingExecutedByParser = funcBeingExecuted;
                currentDepthLevel += 1;
            }

            else
                isCurrentlyExecutingFunction = false;

            //If we are in the main script actually being compiled, set the global script token list
            if (!isInFunction && !isInExternalScript)
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

                            //Set the depth level of the new variable to the current depth level in the script
                            newVariable.depthLevel = currentDepthLevel;

                            //This variable is a usable variable that was declared in this script
                            usableVariables.Add(newVariable);

                            //Skip the next iteration
                            i += 1;
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
                            //If the token 2 ahead is not an opening parenthesis, throw an error
                            else if (tokenList[i + 2].type != TokenType.Parenthesis && tokenList[i+2].value.ToCharArray()[0] != '(')
                                CrossoverCompiler.ThrowCompilerError("Parameters must be specified with a function declaration.", tokenList[i].isOnLine);


                            //If the following token is an identifier and has an opening parenthesis
                            else
                            {
                                //If this is not the first token of the script, and if the previous token is an 'exclusive' keyword
                                if (i > 0 && tokenList[i - 1].type == TokenType.ExclusiveKeyword)
                                    newFunction.scope = AccessLevel.Exclusive;

                                //Use the following identifier as the new variable's name
                                newFunction.name = tokenList[i + 1].value;
                                usableFunctions.Add(newFunction);

                                //Skip the next iteration (the identifier that we just handled) and advance to the opening parenthesis
                                i += 2;

                                //Handle defined parameters ('i' should be the iteration containing the open parenthesis)
                                //Set the iteration to whatever it is after the parameters are handled
                                i = HandleDefinedParameters(i, newFunction, scriptTokenList);
                            }

                            break;
                        }
                        break;

                    #endregion
                    //If token is an 'if' loop declaration
                    case TokenType.IfKeyword:
#region
                        //Advance past the 'if' token
                        i += 1;

                        //Will contain the contents of the if loop itself
                        List<Token> loopContents = new List<Token>();

                        //Will contain the contents of the items being compared
                        List<Token> comparisonContents = new List<Token>();

                        //While the 'if' loop has not been opened yet
                        //We will get the contents of the comparison
                        while (tokenList[i].value != "{")
                        {
                            //Add the current token to the list of comparison contents
                            comparisonContents.Add(tokenList[i]);

                            //Go to the next token in the list
                            i++;
                        }

                        //If current token is not an opening curly brace, throw error
                        if (tokenList[i].value != "{")
                            CrossoverCompiler.ThrowCompilerError("If loop must start with an opening curly brace", tokenList[i].isOnLine);

                        //Advance past the opening curly brace
                        i += 1;

                        //Set the starting depth level to the current depth level
                        int startingDepthLevel = currentDepthLevel;

                        //Go deeper by one level since we are in a new loop
                        currentDepthLevel += 1;

                        //Get the contents of the 'if' code block
                        while (currentDepthLevel != startingDepthLevel)
                        {
                            Token currentToken = tokenList[i];

                            //If we are starting a new loop within the if loop, go deeper by one level
                            if (currentToken.value == "{")
                                currentDepthLevel += 1;

                            //If we are ending a loop within the if loop, go shallower by one level
                            if (currentToken.value == "}")
                            {
                                //If the level after this closing brace is the level we started at
                                if (currentDepthLevel - 1 == startingDepthLevel)
                                {
                                    //Go shallower a level
                                    currentDepthLevel -= 1;

                                    //Exit out of the while loop
                                    break;
                                }
                            }

                            //Add the token to the loop's contents
                            loopContents.Add(currentToken);

                            //Go to the next token
                            i++;
                        }

                        //
                        // Handle replacing the variables in the comparison with the values of the variables
                        //

                        //Foreach token in the comparison
                        foreach (Token tkn in comparisonContents)
                        {
                            //If the token is an identifier
                            if (tkn.type == TokenType.Identifier)
                            {
                                //Search for the variable being referenced
                                Variable resultOfVarSearch = GetVariableByName(tkn.value, tkn.isOnLine, isInExternalScript);

                                //Assign this token's value and type to the value and type of the referenced variable
                                tkn.value = resultOfVarSearch.value;
                                tkn.type = resultOfVarSearch.type;
                            }
                        }

                        //
                        // Handle the actual comparison and run the code inside the 'if' loop if necessary
                        //

                        //Get the result of the comparison within the 'if' loop
                        bool resultOfComparison = HandleComparison(comparisonContents);

                        //If the resultOfComparison is true
                        if (resultOfComparison)
                        {
                            //Run the code inside the if loop
                            RunBlock(loopContents);
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
                            bool havePrinted = false;

                            //If we are in a function (we need to look for parameters)
                            if (isInFunction)
                            {
                                //Loop through this function's parameters
                                foreach (Parameter param in funcBeingExecuted.parameters)
                                {
                                    //If the parameter's variable matches the name of the variable we are looking for
                                    if (param.parameterVariable.name == tokenList[i + 1].value)
                                    {
                                        //Write the value of the parameter to the console
                                        Console.WriteLine(param.parameterVariable.value);
                                        havePrinted = true;
                                    }
                                }
                            }

                            //If we are in a function and we haven't printed yet (we didn't find any parameters)
                            if (!havePrinted)
                            {
                                Variable varGotten = GetVariableByName(followingToken.value, followingToken.isOnLine, isInExternalScript);

                                //If the variable with the matching name is currently accessible
                                if (varGotten.depthLevel <= currentDepthLevel && !varGotten.isExclusive)
                                {
                                    //Write the value of the variable to the console
                                    Console.WriteLine(varGotten.value);
                                }

                                //If the variable is NOT accessible
                                else
                                    CrossoverCompiler.ThrowCompilerError("Variable " + varGotten.name + " is not accessible in that scope", followingToken.isOnLine);
                            }

                            //Skip the next iteration
                            i += 1;
                        }

                        //Else if we are passed an external declaration (to print external variables)
                        if (followingToken.type == TokenType.ExternalKeyword)
                        {
                            Variable varToPrint = new Variable();

                            Token nameOfExternalScript = tokenList[i + 3];
                            Token externalVariableToFind = tokenList[i + 5];
                            bool canFindExternalScript = false;
                            bool canFindExternalVariable = false;

                            foreach (Variable var in externalVariables)
                            {
                                //If the function's parent script match the one specified in the identifier
                                if (var.parentScript == nameOfExternalScript.value)
                                {
                                    canFindExternalScript = true;

                                    //If the token after the second period matches any of the external functions
                                    if (externalVariableToFind.value == var.name && !var.isExclusive)
                                    {
                                        //Set the variable that we will print to the variable that we just found
                                        canFindExternalVariable = true;
                                        varToPrint = var;
                                    }
                                }
                            }

                            //If the correct variable has been found
                            if (canFindExternalVariable)
                            {
                                //Print its value
                                Console.WriteLine(varToPrint.value);
                            }

                            //If the external script with that name could not be found, throw an error
                            if (!canFindExternalScript)
                                CrossoverCompiler.ThrowCompilerError("Could not find external script imported as: " + nameOfExternalScript.value, tokenList[i].isOnLine);

                            //If the external variable with that name could not be found, throw an error
                            if (!canFindExternalVariable)
                                CrossoverCompiler.ThrowCompilerError("Could not find external variable named: " + externalVariableToFind.value + " ... is the variable exclusive?", tokenList[i].isOnLine);

                            //Advance past this print's token contents
                            i += 5;
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

                                    //If the token after the second period matches any of the external functions AND it's a public function
                                    if (externalVariableOrFunctionToFind.value == func.name && func.scope == AccessLevel.Public)
                                    {
                                        //We've found an external function
                                        canFindExternalVariableOrFunction = true;

                                        //Advance to the iteration containing the name of the function to call
                                        i += 4;

                                        //Handle the function's parameters, set the current iteration to whatever it is after the parameters are evaluated
                                        i = HandlePassedParameters(i, func);

                                        //Run the function after its parameters have been handled
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
                                CrossoverCompiler.ThrowCompilerError("Could not find external function with the name: " + externalVariableOrFunctionToFind.value + " ... is the function exclusive?", tokenList[i].isOnLine);
                            }

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

                        //If we are executing a function right now (we will check for parameters)
                        if (isInFunction)
                        {
                            //If we haven't already found a variable or function of the name of the identifier was found (we will look for parameters)
                            if (!foundVariableOrFunction)
                            {
                                //Foreach parameter from the function
                                foreach (Parameter param in funcBeingExecuted.parameters)
                                {
                                    //If the currently iterated variable's name equals the value of the current identifier AND its depth level is less than or equal to the current depth level
                                    if (param.parameterVariable.name == currentIdentifier.value && param.parameterVariable.depthLevel <= currentDepthLevel)
                                    {
                                        //We've found the parameter we're looking for
                                        foundVariableOrFunction = true;
                                    }

                                    //If the name of the variable matches, but it isn't accessible at the current depth level
                                    else if (param.parameterVariable.name == currentIdentifier.value && param.parameterVariable.depthLevel > currentDepthLevel)
                                    {
                                        //Throw an error
                                        CrossoverCompiler.ThrowCompilerError(param.parameterVariable.name + " is not accessible in the current scope", currentIdentifier.isOnLine);
                                    }
                                }
                            }

                            //If no PARAMETER of the name of the identifier was found (we will look for variables)
                            if (!foundVariableOrFunction)
                            {
                                //Foreach usable variable
                                foreach (Variable var in usableVariables)
                                {
                                    //If the currently iterated variable's name equals the value of the current identifier AND its depth level matches the current depth level
                                    if (var.name == currentIdentifier.value && var.depthLevel <= currentDepthLevel)
                                    {
                                        //We've found the variable we're looking for
                                        foundVariableOrFunction = true;
                                    }

                                    //If the name of the variable matches, but it isn't accessible at the current depth level
                                    else if (var.name == currentIdentifier.value && var.depthLevel != currentDepthLevel)
                                    {
                                        //Throw an error
                                        CrossoverCompiler.ThrowCompilerError(var.name + " is not accessible in the current scope", currentIdentifier.isOnLine);
                                    }
                                }
                            }

                            //If we haven't found a variable or function with a matching name, throw error
                            if (!foundVariableOrFunction)
                                CrossoverCompiler.ThrowCompilerError("Could not find variable, function, or parameter with name: " + currentIdentifier.value, tokenList[i].isOnLine);
                        }

                        //Foreach locally accessible function (external function checks are handled in the 'external' token case)
                        foreach (Function func in usableFunctions)
                        {
                            //If the name of the function currently iterated equals the value of the identifier (the current token)
                            if (func.name == currentIdentifier.value)
                            {
                                foundVariableOrFunction = true;

                                //Handle the function's parameters, set the current iteration to whatever it is after the parameters are evaluated
                                i = HandlePassedParameters(i, func);

                                //Run the function after its parameters have been handled
                                RunFunction(func, false);
                            }
                        }

                        //If no FUNCTION of the name of the identifier was found (we will look for variables)
                        if (!foundVariableOrFunction)
                        {
                            //Foreach usable variable
                            foreach (Variable var in usableVariables)
                            {
                                //If the currently iterated variable's name equals the value of the current identifier AND its depth level matches the current depth level
                                if (var.name == currentIdentifier.value && var.depthLevel <= currentDepthLevel)
                                {
                                    //We've found the variable we're looking for
                                    foundVariableOrFunction = true;
                                }

                                //If the name of the variable matches, but it isn't accessible at the current depth level
                                else if (var.name == currentIdentifier.value && var.depthLevel != currentDepthLevel)
                                {
                                    //Throw an error
                                    CrossoverCompiler.ThrowCompilerError(var.name + " is not accessible in the current scope", currentIdentifier.isOnLine);
                                }
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
                                int beginningDepthLevel = currentDepthLevel;

                                //Go deeper by one
                                currentDepthLevel += 1;

                                //Function contents have started
                                waitingForFunctionStart = false;
                                waitingForFunctionEnd = true;

                                //Go to next token
                                i += 1;

                                //While the function is still open and has not been closed with a brace yet
                                while (currentDepthLevel != beginningDepthLevel)
                                {
                                    //If the token is an opening brace, add one to the depth level
                                    if (tokenList[i].value == "{")
                                        currentDepthLevel += 1;

                                    //If it's a closing brace, subtract one from the depth level
                                    else if (tokenList[i].value == "}")
                                    {
                                        currentDepthLevel -= 1;

                                        //If the brace closes the function, break before it is added to the function's contents
                                        if (currentDepthLevel == beginningDepthLevel)
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
                    //If the token is an equals (=)
                    case TokenType.Equals:
#region
                        Token variableToken = tokenList[i - 1];
                        List<Token> setToTokens = new List<Token>();

                        //If the previous token is not an identifier, throw error
                        if (variableToken.type != TokenType.Identifier)
                            CrossoverCompiler.ThrowCompilerError("Cannot assign values to non-variables", variableToken.isOnLine);
                        
                        //
                        //Handle GETTING the variable
                        //

                        Variable varToSet = new Variable();
                        bool foundVar = false;

                        //If we are in a function (check if the variable we are setting is a parameter)
                        if (isInFunction)
                        {
                            Variable resultOfParamSearch = GetParameterByName(variableToken.value, funcBeingExecuted);

                            //If the parameter seach did NOT return null
                            if (resultOfParamSearch != null)
                            {
                                //Set the variable to the parameter that we found
                                varToSet = resultOfParamSearch;
                                foundVar = true;
                            }
                        }

                        //If we did not find a parameter (either we are not in a function or no parameter with that name existed)
                        if (!foundVar)
                        {
                            //Get the variable that we are setting
                            varToSet = GetVariableByName(variableToken.value, variableToken.isOnLine, isInExternalScript);
                            foundVar = true;
                        }

                        //
                        //Handle SETTING the variable
                        //

                        //Advance to the token following the equals
                        i += 1;

                        //While there are tokens in between the equals and the line ending (get all the tokens we are setting equal to)
                        while (tokenList[i].type != TokenType.LineEnding)
                        {
                            //Add the current token to the list of tokens that we are setting equal to
                            setToTokens.Add(tokenList[i]);

                            //Go to the next token
                            i++;
                        }

                        //If we are only setting equal to one token
                        if (setToTokens.Count == 1)
                        {
                            //Get the first and only token in the list
                            Token tokenToSetTo = setToTokens[0];

                            //Switch through the possible TokenTypes
                            switch (tokenToSetTo.type)
                            {
                                //If token is an identifier (setting this variable's value to another variable's value)
                                case TokenType.Identifier:
                                    bool foundVarToSetTo = false;

                                    //If we are in a function (look for a parameter with that name)
                                    if (isInFunction)
                                    {
                                        Variable resultOfParamSearch = GetParameterByName(tokenToSetTo.value, funcBeingExecuted);

                                        //If the parameter seach did NOT return null
                                        if (resultOfParamSearch != null)
                                        {
                                            //Set the variable's type and value to the parameter's type and value
                                            varToSet.value = resultOfParamSearch.value;
                                            varToSet.type = resultOfParamSearch.type;
                                            foundVarToSetTo = true;
                                        }
                                    }

                                    //If we're not in a function or we didn't find a parameter
                                    if (!foundVarToSetTo)
                                    {
                                        Variable resultOfVariableSearch = GetVariableByName(tokenToSetTo.value, tokenToSetTo.isOnLine, isInExternalScript);

                                        //Set the variable's type and value to the type and value of the variable that we found
                                        varToSet.value = resultOfVariableSearch.value;
                                        varToSet.type = resultOfVariableSearch.type;
                                        foundVar = true;
                                    }

                                    break;

                                //If token is a string, bool, int, or float value (giving this variable a raw value)
                                case TokenType.StringVariable:
                                case TokenType.BoolVariable:
                                case TokenType.IntVariable:
                                case TokenType.FloatVariable:

                                    //Set the variable's value and type to this value and type
                                    varToSet.value = tokenToSetTo.value;
                                    varToSet.type = tokenToSetTo.type;

                                    break;

                                //If no valid type can be found, throw an error
                                default:
                                    CrossoverCompiler.ThrowCompilerError(string.Format("Could not set variable {0} equal to {1}", variableToken.value, tokenToSetTo.value), tokenToSetTo.isOnLine);
                                    break;
                            }
                        }

                        //If we are setting the variable to more than one token (handles math)
                        else if (setToTokens.Count > 1)
                        {
                            //Can remove this inner 'if' loop and just move everything inside of the bigger loop
                            //If there are more than 3 tokens following the equals
                            if (setToTokens.Count >= 3)
                            {
                                //Check for variables passed and replace them with their values
                                //For every token that was passed
                                for (int x = 0; x < setToTokens.Count; x++)
                                {
                                    Token currentToken = setToTokens[x];

                                    //If the current token is an identifier
                                    if (currentToken.type == TokenType.Identifier)
                                    {
                                        bool foundVarToSetTo = false;

                                        //If we are in a function (look for a parameter with that name)
                                        if (isInFunction)
                                        {
                                            Variable resultOfParamSearch = GetParameterByName(currentToken.value, funcBeingExecuted);

                                            //If the parameter seach did NOT return null
                                            if (resultOfParamSearch != null)
                                            {
                                                //Replace the token with the value of the parameter found
                                                currentToken.value = resultOfParamSearch.value;
                                                currentToken.type = resultOfParamSearch.type;
                                                foundVarToSetTo = true;
                                            }
                                        }

                                        //If we're not in a function or we didn't find a parameter
                                        if (!foundVarToSetTo)
                                        {
                                            Variable resultOfVariableSearch = GetVariableByName(currentToken.value, currentToken.isOnLine, isInExternalScript);

                                            //Replace the token with the value of the variable found
                                            currentToken.value = resultOfVariableSearch.value;
                                            currentToken.type = resultOfVariableSearch.type;
                                            foundVar = true;
                                        }
                                    }
                                }

                                //
                                //ERROR CHECKS
                                //

                                //Check syntax
                                //For each token following the equals
                                for (int a = 0; a < setToTokens.Count; a++)
                                {
                                    //If the iteration is 0 or an even number
                                    if (a == 0 || a % 2 == 0)
                                    {
                                        //If the token is NOT a raw value type or an identifier, throw error
                                        if (setToTokens[a].type != TokenType.Identifier && setToTokens[a].type != TokenType.StringVariable && setToTokens[a].type != TokenType.BoolVariable && setToTokens[a].type != TokenType.IntVariable && setToTokens[a].type != TokenType.FloatVariable)
                                            CrossoverCompiler.ThrowCompilerError("Invalid expression", setToTokens[a].isOnLine);
                                    }

                                    //If the iteration is an odd number
                                    else
                                    {
                                        //If the token is NOT an operator, throw error
                                        if (setToTokens[a].type != TokenType.MathematicalOperator)
                                            CrossoverCompiler.ThrowCompilerError("Invalid expression", setToTokens[a].isOnLine);
                                    }
                                }

                                //Check if each math token is operable with every other one involved in the expression (two-dimensional loop)
                                //For each token following the equals (first dimension)
                                for (int x = 0; x < setToTokens.Count; x++)
                                {
                                    //If the token from the first dimension is NOT an operator (we don't want to check if 'operators' are operable)
                                    if (setToTokens[x].type != TokenType.MathematicalOperator)
                                    {
                                        //AGAIN, for each token following the equals (second dimension)
                                        for (int y = 0; y < setToTokens.Count; y++)
                                        {
                                            //If the token from the second dimension is NOT an operator (we don't want to check if 'operators' are operable)
                                            if (setToTokens[y].type != TokenType.MathematicalOperator)
                                            {
                                                //If the tokens from each dimension are NOT operable, throw error
                                                if (!CheckOperable(setToTokens[x], setToTokens[y]))
                                                    CrossoverCompiler.ThrowCompilerError("Inoperable types", setToTokens[x].isOnLine);
                                            }
                                        }
                                    }
                                }

                                //
                                //END ERROR CHECKS
                                //

                                //Try to evaluate the tokens following the equals
                                Variable setToVariable = TryEvaluate(setToTokens);

                                //Set the variable's value and type to the evaluated value and type
                                varToSet.value = setToVariable.value;
                                varToSet.type = setToVariable.type;
                            }
                        }

                        //If zero tokens were passed, throw error
                        else
                            CrossoverCompiler.ThrowCompilerError("Invalid expression", tokenList[i].isOnLine);

                        break;
#endregion
                    //Default exit
                    default:
                        break;
                }

                //If the depth level is greater than 0
                if (currentDepthLevel > 0)
                {
                    //If this is the last token in the current token list
                    if (i == tokenList.Count - 1)
                    {
                        //Go down a depth level
                        currentDepthLevel -= 1;
                    }
                }
            }
        }

        //Handles parameters when they are specified in a function's definition
        int HandleDefinedParameters(int currentIteration, Function funcToHandle, List<Token> tokenListToUse)
        {
            List<Token> detectedTokensWithinParentheses = new List<Token>();
            List<Parameter> detectedParametersWithinParentheses = new List<Parameter>();

            //Depth level of parentheses within parameters
            int parenthesisDepthLevel = 1;

            //Go to the token following the opening parenthesis
            currentIteration += 1;

            //
            //  GET TOKENS
            //

            //Loop through all tokens until the original parameter block of parentheses is closed
            while (parenthesisDepthLevel != 0)
            {
                //If the token is an opening parenthesis, add one to the depth level
                if (tokenListToUse[currentIteration].type == TokenType.Parenthesis && tokenListToUse[currentIteration].value == "(")
                {
                    parenthesisDepthLevel += 1;
                    currentIteration++;
                }

                //If the token is a closing parenthesis, subtract one from the depth level
                else if (tokenListToUse[currentIteration].type == TokenType.Parenthesis && tokenListToUse[currentIteration].value == ")")
                {
                    parenthesisDepthLevel -= 1;
                    currentIteration++;
                }

                //Otherwise, add the token to the list of tokens within the parentheses and move to the next token
                else
                {
                    detectedTokensWithinParentheses.Add(tokenListToUse[currentIteration]);
                    currentIteration++;
                }
            }

            //
            //  INITIALIZE PARAMETERS
            //

            //Index used to determine where we are in param definition ('var' = 1, identifier = 2, comma = 3)
            int parameterPatternIndex = 1;

            Variable paramVar = new Variable();

            for (int i = 0; i < detectedTokensWithinParentheses.Count; i++)
            {
                Token token = detectedTokensWithinParentheses[i];

                //If we are on the first index and the token is a variable declaration (correct)
                if (parameterPatternIndex == 1 && token.type == TokenType.VariableDeclaration)
                {
                    //Set this parameter's variable's depth level to 1 (it's only accessible within function)
                    paramVar.depthLevel = 1;

                    //Go to the next index
                    parameterPatternIndex++;
                }

                //If we are on the first index and the token is NOT a variable declaration (error)
                else if (parameterPatternIndex == 1 && token.type != TokenType.VariableDeclaration)
                    CrossoverCompiler.ThrowCompilerError("Parameters must be initialized with a 'var' keyword, followed by an identifier, when being defined.", token.isOnLine);

                //If we are on the second index and the token is an identifier and this is the last token within the parentheses (correct)
                else if (parameterPatternIndex == 2 && token.type == TokenType.Identifier && i == detectedTokensWithinParentheses.Count - 1)
                {
                    //Set the name of parameter to the identifier
                    paramVar.name = token.value;

                    //Add the parameter to the function's parameters
                    funcToHandle.parameters.Add(new Parameter(paramVar));
                }

                //If we are on the second index and the token is an identifier (correct)
                else if (parameterPatternIndex == 2 && token.type == TokenType.Identifier)
                {
                    //Set the name of parameter to the identifier
                    paramVar.name = token.value;

                    //Go to the next token
                    parameterPatternIndex++;
                }

                //If we are on the second index and the token is NOT an identifier (error)
                else if (parameterPatternIndex == 2 && token.type != TokenType.VariableDeclaration)
                    CrossoverCompiler.ThrowCompilerError("Parameters must be initialized with a 'var' keyword, followed by an identifier, when being defined.", token.isOnLine);

                //If we are on the third index and the token is a comma (correct)
                else if (parameterPatternIndex == 3 && token.type == TokenType.Comma)
                {
                    //Add the parameter to the function's parameters
                    funcToHandle.parameters.Add(new Parameter(paramVar));

                    //Reset the paramVar variable
                    paramVar = new Variable();

                    //Reset the pattern index so that we can look for the next parameter
                    parameterPatternIndex = 1;
                }

                //If we are on the third index and the token is NOT an comma (error)
                else if (parameterPatternIndex == 3 && token.type != TokenType.Comma)
                    CrossoverCompiler.ThrowCompilerError("Parameters must be initialized with a 'var' keyword, followed by an identifier, with parameters being separated by commas when being defined.", token.isOnLine);
            }


            //Return the updated iteration number - 1 to restart on the opening curly brace
            return currentIteration - 1;
        }

        //Handles parameters when they are passed into a function call
        int HandlePassedParameters(int currentIteration, Function funcToHandle)
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
                if (scriptTokenList[currentIteration].type == TokenType.Parenthesis && scriptTokenList[currentIteration].value == "(")
                {
                    parenthesisDepthLevel += 1;
                    currentIteration++;
                }

                //If the token is a closing parenthesis, subtract one from the depth level
                else if (scriptTokenList[currentIteration].type == TokenType.Parenthesis && scriptTokenList[currentIteration].value == ")")
                {
                    parenthesisDepthLevel -= 1;
                    currentIteration++;
                }

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

            //Check how many parameters there are
            //Foreach token in all the tokens that were detected within the parentheses of the parameters
            for (int i = 0; i < detectedTokensWithinParentheses.Count; i++)
            {
                Token token = detectedTokensWithinParentheses[i];

                //If the token is a comma, add a new PassedParameter to the list
                if (token.type == TokenType.Comma)
                    detectedParametersWithinParentheses.Add(new PassedParameter());

                //If the token is the last one within the parethesis, add a new PassedParameter to the list
                if (i == detectedTokensWithinParentheses.Count - 1)
                    detectedParametersWithinParentheses.Add(new PassedParameter());
            }

            //detectedParametersWithinParentheses should now only be as long as needed

            //Set all of the parameters
            //For each parameter that we need to set
            for (int i = 0; i < detectedParametersWithinParentheses.Count; i++)
            {
                //For each token in all the tokens that were detected within the parentheses of the parameters
                for (int x = 0; x < detectedTokensWithinParentheses.Count; x++)
                {
                    Token token = detectedTokensWithinParentheses[x];

                    //If the token is a comma
                    if (token.type == TokenType.Comma)
                    {
                        //Go to the next token
                        i++;
                    }

                    //If the token is anything but a comma
                    else
                    {
                        //Add it to the contents of the current parameter
                        detectedParametersWithinParentheses[i].contents.Add(token);
                    }

                    //If the token is the last one within the paretheses
                    if (i == detectedTokensWithinParentheses.Count - 1)
                    {
                        //Add it to the contents of the last parameter
                        detectedParametersWithinParentheses[detectedParametersWithinParentheses.Count - 1].contents.Add(token);
                    }
                }
            }

            //If the amount of parameters passed doesn't equal the amount of parameters that the function takes in, throw an error
            if (detectedParametersWithinParentheses.Count != funcToHandle.parameters.Count)
                CrossoverCompiler.ThrowCompilerError("Function " + funcToHandle.name + " takes in " + funcToHandle.parameters.Count + " parameters, but was passed " + detectedParametersWithinParentheses.Count + " when the function was called.", scriptTokenList[currentIteration].isOnLine);


            //
            //  EVALUATE PARAMETERS
            //


            //Foreach parameter detected, try to evaluate it
            //Should work because there is an error catch if the detected parameters != the function's specified parameters
            for (int i = 0; i < detectedParametersWithinParentheses.Count; i++)
            {
                PassedParameter param = detectedParametersWithinParentheses[i];

                //If the parameter contains more than one token, try to evaluate it into a valid value to pass to the function
                if (param.contents.Count > 1)
                {
                    //Foreach token in the parameter's contents
                    foreach (Token token in param.contents)
                    {
                        //If the token is an identifier, get the variable of its name and pass that to the contents
                        if (token.type == TokenType.Identifier)
                        {
                            Variable theVar = new Variable();

                            //Handles passes of parameters from a function if this function is being called from inside another function

                            //If we are in a function (we need to look for parameters as well)
                            if (isCurrentlyExecutingFunction)
                            {
                                //Loop through this function's parameters
                                foreach (Parameter functionsParameter in functionBeingExecutedByParser.parameters)
                                {
                                    //If the parameter's variable matches the name of the variable we are looking for
                                    if (functionsParameter.parameterVariable.name == scriptTokenList[i - 2].value)
                                    {
                                        //Set the parameter's variable's type and value equal to this token's type and value
                                        theVar.type = scriptTokenList[i].type;
                                        theVar.value = scriptTokenList[i].value;
                                    }
                                }
                            }

                            //If we are not in a function
                            else
                            {
                                //Get the variable with the identifier's name
                                theVar = GetVariableByName(scriptTokenList[i - 2].value, scriptTokenList[i - 2].isOnLine, false);

                                //Set the variable's type and value equal to this token's type and value
                                theVar.type = scriptTokenList[i].type;
                                theVar.value = scriptTokenList[i].value;
                            }
                        }
                        
                        //TODO: Check for external variable being passed
                    }

                    //Try to evaluate the contents of the parameter into a single value and store it in a Variable
                    Variable evaluatedVariable = TryEvaluate(param.contents);

                    //If the parameter's contents could be evaluated into a single value
                    if (evaluatedVariable != null)
                    {
                        //Pass the value of the TryEvaluate() to the function
                        funcToHandle.parameters[i].parameterVariable.value = evaluatedVariable.value;
                    }

                    //If the evaluation failed
                    else
                        CrossoverCompiler.ThrowCompilerError("Could not evaluate contents of parameter.", scriptTokenList[currentIteration].isOnLine);
                }

                //If the parameter is only one token
                else
                {
                    //Local variable storing the value of the current parameter
                    Token paramValue = param.contents[0];


                    //Set the type of the function's defined parameter to the passed parameter's type
                    funcToHandle.parameters[i].parameterVariable.type = paramValue.type;

                    //If the Token is an identifier, set the value of the parameter in the function to the value of the variable passed
                    if (paramValue.type == TokenType.Identifier)
                        funcToHandle.parameters[i].parameterVariable = GetVariableByName(paramValue.value, paramValue.isOnLine, false);

                    //If the Token is a RAW VALUE, set the value of the parameter in the function to the raw value passed in the parameter
                    if (paramValue.type == TokenType.IntVariable || paramValue.type == TokenType.FloatVariable || paramValue.type == TokenType.BoolVariable || paramValue.type == TokenType.StringVariable)
                        funcToHandle.parameters[i].parameterVariable.value = paramValue.value;
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

            string tokensValueString = UtilityTools.TokenListToString(tokensToEvaluate);
            OperationType operationToPerform = new OperationType();

            //For every token in the tokens that need to be evaluated
            for (int i = 0; i < tokensToEvaluate.Count; i++)
            {
                Token currentToken = tokensToEvaluate[i];

                //Error check
                //If the token is NOT of an operable type, throw an error
                if (currentToken.type != TokenType.IntVariable && currentToken.type != TokenType.FloatVariable && currentToken.type != TokenType.StringVariable && currentToken.type != TokenType.MathematicalOperator)
                    CrossoverCompiler.ThrowCompilerError("Cannot operate on this data type.", currentToken.isOnLine);
                if (currentToken.type == TokenType.MathematicalOperator)
                    if (i == 0)
                        CrossoverCompiler.ThrowCompilerError("The first value in an expression must be an operable data type, not an operator.", currentToken.isOnLine);


                //If it's the first token that we need to evaluate, set it as the firstToken
                if (i == 0)
                    firstToken = currentToken;

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
                                //Designate which operation to perform
                                operationToPerform = OperationType.Num;

                                break;

                            case TokenType.StringVariable:
                                //Designate which operation to perform
                                operationToPerform = OperationType.String;

                                //If the operater that we're going to use is not a plus, throw an error
                                if (operatorToUse.value != '+'.ToString())
                                    CrossoverCompiler.ThrowCompilerError("Strings can only be added to.", currentToken.isOnLine);

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


            //If the operation is on numbers
            if (operationToPerform == OperationType.Num)
            {
                string resultOfComputation = dtTool.Compute(tokensValueString, "").ToString();

                //Compute result of expression
                varToReturn.value = resultOfComputation;

                //If the result of the computation is an integer
                if (int.TryParse(resultOfComputation, out int n))
                {
                    //Set the type of the variable being returned to an int
                    varToReturn.type = TokenType.IntVariable;
                }

                //If the result of the computation is NOT an integer
                else
                {
                    //Set the type of the variable being returned to a float
                    varToReturn.type = TokenType.FloatVariable;
                }
            }

            else if (operationToPerform == OperationType.String)
            {
                //Set the type of the variable being returned to StringVariable
                varToReturn.type = TokenType.StringVariable;

                //Foreach token to operate on
                foreach (Token tkn in tokensToEvaluate)
                {
                    //If the token is a string value, add it to the value to return
                    if (tkn.type == TokenType.StringVariable)
                        varToReturn.value += tkn.value;
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

        //Checks if a comparison returns true or false
        bool HandleComparison(List<Token> comparisonTokens)
        {
            //Foreach token that is a part of the comparison
            for (int i = 0; i < comparisonTokens.Count; i++)
            {
                Token currentToken = comparisonTokens[i];

                //If the token is an equals sign
                if (currentToken.type == TokenType.Equals)
                {
                    //If the following token is a boolean value
                    if (comparisonTokens[i+1].type == TokenType.BoolVariable)
                    {
                        //If the previous token equals the value of the following token
                        if (comparisonTokens[i - 1].value == comparisonTokens[i + 1].value)
                        {
                            return true;
                        }

                        //If they are not equal, return false
                        else
                            return false;
                    }
                }
            }

            return false;
        }

        //Use to execute a function
        public void RunFunction(Function functionToRun, bool functionIsExternal)
        {
            ActOnTokens(functionToRun.contents, true, functionToRun, functionIsExternal);
        }

        //Use to execute a block of code
        public void RunBlock(List<Token> blockToRun)
        {
            ActOnTokens(blockToRun, false, null, false);
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

                            //Add it to the accessible external variables
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

                            //Use identifier as the new variable's name and skip the next iteration
                            newFunction.name = tokensToCheck[i + 1].value;

                            //Set parent script to the file that the function comes from
                            newFunction.parentScript = fileName;

                            //Add the function to the external functions
                            externalFunctions.Add(newFunction);

                            //Skip the next iteration (the identifier that we just handled) and advance to the opening parenthesis
                            i += 2;

                            //Handle defined parameters ('i' should be the iteration containing the open parenthesis)
                            //Set the iteration to whatever it is after the parameters are handled
                            i = HandleDefinedParameters(i, newFunction, tokensToCheck);
                            i -= 1;

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
                            //If the previous token is an equals AND not the 'use' keyword AND we are NOT in parameters, if the token 2 iterations back is an identifier
                            if (tokensToCheck[i - 1].type == TokenType.Equals && tokensToCheck[i - 1].type != TokenType.UseKeyword && !inParams)
                            {
                                //If the token 2 back is an identifier
                                if (tokensToCheck[i - 2].type == TokenType.Identifier)
                                {
                                    Variable theVar = new Variable();

                                    //Get the variable with the identifier's name
                                    theVar = GetVariableByName(tokensToCheck[i - 2].value, tokensToCheck[i - 2].isOnLine, true);

                                    //If the next token is an operator
                                    if (tokensToCheck[i + 1].type == TokenType.MathematicalOperator)
                                    {
                                        //Check if the tokens are operable
                                        if (CheckOperable(tokensToCheck[i], tokensToCheck[i + 2]))
                                        {
                                            List<Token> tokensToEval = new List<Token>();

                                            //Add each token that we need to evaluate to tokensToEval
                                            for (int x = 0; x < 3; x++)
                                                tokensToEval.Add(tokensToCheck[i + x]);

                                            //Try to evaluate the expression
                                            Variable evaluatedVar = TryEvaluate(tokensToEval);

                                            //Set the value of the variable to the value of the evaluated expression
                                            theVar.type = evaluatedVar.type;
                                            theVar.value = evaluatedVar.value;
                                        }
                                    }

                                    //If the next token is a line ending
                                    else if (tokensToCheck[i + 1].type == TokenType.LineEnding)
                                    {
                                        //Set the parameter's variable's type and value equal to this token's type and value
                                        theVar.type = tokensToCheck[i].type;
                                        theVar.value = tokensToCheck[i].value;
                                    }

                                    //If the next token is not an operator or a line ending, throw error
                                    else
                                        CrossoverCompiler.ThrowCompilerError("Could not set variable: " + theVar.name, tokensToCheck[i].isOnLine);
                                }
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

        //Use to get a parameter by name in a function
        public Variable GetParameterByName(string paramName, Function getParamsFromFunc)
        {
            //Foreach of the function's parameters
            foreach (Parameter param in getParamsFromFunc.parameters)
            {
                //If the name of the parameter matches the name we're looking for
                if (param.parameterVariable.name == paramName)
                {
                    //Return that parameter
                    return param.parameterVariable;
                }
            }

            return null;
        }
    }
}