using System;
using System.Collections.Generic;
using Crossover;

namespace Crossover
{
    class Parser
    {
        List<Variable> usableVariables = new List<Variable>();
        List<Function> usableFunctions = new List<Function>();

        //List of externally imported functions
        List<Function> externalFunctions;

        //Number representing "depth", or what number nested loop we are in right now (e.g. blank program is 0, inside function is 1, inside if loop inside function is 2)
        int currentDepthLevel = 0;

        bool waitingForFunctionStart = false;
        bool waitingForFunctionEnd = false;

        public void ActOnTokens(List<Token> tokenList, bool isInFunction)
        {
            //For each token in tokenList
            for (int i = 0; i < tokenList.Count; i++)
            {
                //Check TokenType
                switch (tokenList[i].type)
                {
                    //If token is a variable declaration ('var')
                    case TokenType.VariableDeclaration:
                        Variable newVariable = new Variable();

                        //If the following token is not an identifier, then throw an error
                        if (tokenList[i + 1].type != TokenType.Identifier)
                            CrossoverCompiler.ThrowCompilerError("Variable must be named immediately after it is declared", tokenList[i].isOnLine);
                        else
                        {
                            if (tokenList[i - 1].type == TokenType.ExclusiveKeyword)
                                newVariable.isExclusive = true;

                            //If the following token is an identifier, use it as the new variable's name and skip the next iteration
                            newVariable.name = tokenList[i + 1].value;
                            newVariable.depthLevel = currentDepthLevel;
                            usableVariables.Add(newVariable);
                            i += 1;
                        }
                        break;

                    //If token is a function declaration ('function')
                    case TokenType.FunctionDeclaration:
                        Function newFunction = new Function();

                        //We are now awaiting an opening curly brace
                        waitingForFunctionStart = true;

                        //If the following token is not an identifier, then throw an error
                        if (tokenList[i + 1].type != TokenType.Identifier)
                            CrossoverCompiler.ThrowCompilerError("Function must be named immediately after it is declared", tokenList[i].isOnLine);
                        else
                        {
                            if (tokenList[i - 1].type == TokenType.ExclusiveKeyword)
                                newFunction.scope = AccessLevel.Exclusive;

                            //If the following token is an identifier, use it as the new variable's name and skip the next iteration
                            newFunction.name = tokenList[i + 1].value;
                            usableFunctions.Add(newFunction);

                            i += 1;
                        }

                        break;

                    //If token is the 'use' keyword
                    case TokenType.UseKeyword:
                        //If this is being executed inside a function, throw an error
                        if (isInFunction)
                            CrossoverCompiler.ThrowCompilerError("Cannot use 'use' keyword inside of a function", tokenList[i].isOnLine);
                        
                        //Get string from next token and read file from path in string

                        break;

                    //If token is the 'external' keyword
                    case TokenType.ExternalKeyword:
                        //If the following token is not an period, then throw an error
                        if (tokenList[i + 1].type != TokenType.Period)
                            CrossoverCompiler.ThrowCompilerError("external keyword must be followed by a period", tokenList[i].isOnLine);
                        
                        break;
                    
                    //If token is a curly brace
                    case TokenType.CurlyBrace:
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
                    
                    //Default exit
                    default:
                        break;
                }
            }
        }

        public void RunFunction(Function functionToRun)
        {
            ActOnTokens(functionToRun.contents, true);
        }
    }
}