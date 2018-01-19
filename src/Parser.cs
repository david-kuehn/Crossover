using System;
using System.IO;
using System.Collections.Generic;
using Crossover;

namespace Crossover
{
    class Parser
    {
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

                                //If the following token is an identifier, use it as the new variable's name and skip the next iteration
                                newVariable.name = tokenList[i + 1].value;
                                newVariable.depthLevel = currentDepthLevel;
                                usableVariables.Add(newVariable);
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
                            FindFunctionsAndVariablesInExternal(externalInputTokenizer.InputToTokensList(File.ReadAllText(@pathOfExternalFile.value)));

                            break;
                        }

                        else
                        {
                            CrossoverCompiler.ThrowCompilerError("Cannot access other external scripts from an external script", tokenList[i].isOnLine);
                        }

                        break;
                    #endregion
                    //If token is the 'external' keyword
                    case TokenType.ExternalKeyword:
#region
                        if (!isInExternalScript)
                        {
                            Token externalFunctionToFind = tokenList[i + 2];

                            bool canFindExternalFunction = false;

                            //If the following token is not an period, then throw an error
                            if (tokenList[i + 1].type != TokenType.Period)
                                CrossoverCompiler.ThrowCompilerError("'external' keyword must be followed by a period", tokenList[i].isOnLine);

                            //Foreach external function that has been imported from external scripts
                            foreach (Function func in externalFunctions)
                            {
                                //If the token after the period matches any of the external functions
                                if (externalFunctionToFind.value == func.name)
                                {
                                    //Do the function that it matches
                                    canFindExternalFunction = true;
                                    RunFunction(func, true);
                                }
                            }

                            //If external function cannot be found
                            if (!canFindExternalFunction)
                            {
                                //Throw error
                                CrossoverCompiler.ThrowCompilerError("Could not find external function with the name: " + externalFunctionToFind.value, tokenList[i].isOnLine);
                            }

                            i += 2;

                            break;
                        }
                        else
                        {
                            CrossoverCompiler.ThrowCompilerError("Cannot access other external scripts from an external script", tokenList[i].isOnLine);
                        }
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
                    //If token is a string, bool, int, or float variable
                    case TokenType.StringVariable:
                    case TokenType.BoolVariable:
                    case TokenType.IntVariable:
                    case TokenType.FloatVariable:
#region
                        if (!isInExternalScript)
                        {
                            //If the previous token is an equals AND we are NOT in parameters, if the token 2 iterations back is an identifier
                            if (tokenList[i-1].type == TokenType.Equals && !inParams)
                            {
                                if (tokenList[i-2].type == TokenType.Identifier)
                                {
                                    //Get the variable with the identifier's name, and set its value equal to this token's value
                                    GetVariableByName(tokenList[i - 2].value, tokenList[i - 2].isOnLine, false).value = tokenList[i].value;
                                }

                                else
                                {
                                    CrossoverCompiler.ThrowCompilerError("String values must be used to set a valid variable or compare to another string value", tokenList[i].isOnLine);
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
                                    CrossoverCompiler.ThrowCompilerError("String values must be used to set a valid variable or compare to another string value", tokenList[i].isOnLine);
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

        public void RunFunction(Function functionToRun, bool functionIsExternal)
        {
            ActOnTokens(functionToRun.contents, true, functionIsExternal);
        }

        //Use to find functions in external file
        public void FindFunctionsAndVariablesInExternal(List<Token> tokensToCheck)
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