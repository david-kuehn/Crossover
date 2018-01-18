using System;
using System.IO;
using System.Collections.Generic;
using Crossover;

namespace Crossover
{
    class Tokenizer
    {
        string input;
        char[] inputChars;

        //Current character in input string
        int positionInInput;

        //Current line
        int lineIndex = 0;

        //Returns a list of tokens from the input
        public List<Token> InputToTokensList(string _input)
        {

            input = _input;
            inputChars = _input.ToCharArray();

            List<Token> tokensFromInput = new List<Token>();

            //Handle token checks while current position in input is less than the total length
            while (positionInInput < inputChars.Length)
            {
                //If comment is detected
                if (inputChars[positionInInput].Equals((char)47) && inputChars[positionInInput + 1].Equals((char)47))
                {
                    tokensFromInput.Add(CheckForComment());
                }

                //If variable declaration is detected ('var')
                else if (inputChars[positionInInput].Equals('v') && inputChars[positionInInput + 1].Equals('a') && inputChars[positionInInput + 2].Equals('r') && inputChars[positionInInput + 3].Equals(' '))
                {
                    //Add a new Variable Declaration token
                    tokensFromInput.Add(new Token(TokenType.VariableDeclaration, "var", lineIndex));
                    positionInInput += 3;
                }

                //If string is detected (single quote) using Unicode for single quote/apostrophe
                else if (inputChars[positionInInput].Equals((char)8.217) || inputChars[positionInInput].Equals((char)39))
                {
                    tokensFromInput.Add(CheckForString());
                }

                //If 'true' is detected (bool variable)
                else if (inputChars[positionInInput].Equals('t') && inputChars[positionInInput + 1].Equals('r') && inputChars[positionInInput + 2].Equals('u') && inputChars[positionInInput + 3].Equals('e') && !Char.IsLetterOrDigit(inputChars[positionInInput + 4]))
                {
                    tokensFromInput.Add(new Token(TokenType.BoolVariable, "true", lineIndex));

                    //Relocates position to character after 'true' word
                    positionInInput += 4;
                }
                
                //If 'false' is detected (bool variable)
                else if (inputChars[positionInInput].Equals('f') && inputChars[positionInInput + 1].Equals('a') && inputChars[positionInInput + 2].Equals('l') && inputChars[positionInInput + 3].Equals('s') && inputChars[positionInInput + 4].Equals('e') && !Char.IsLetterOrDigit(inputChars[positionInInput + 5]))
                {
                    tokensFromInput.Add(new Token(TokenType.BoolVariable, "false", lineIndex));

                    //Relocates position to character after 'false' word
                    positionInInput += 5;
                }

                //If int or float is detected
                else if (Char.IsDigit(inputChars[positionInInput]))
                {
                    tokensFromInput.Add(CheckForIntOrFloat());
                }

                //If function declaration is detected
                else if (inputChars[positionInInput].Equals('f') && inputChars[positionInInput + 1].Equals('u') && inputChars[positionInInput + 2].Equals('n') && inputChars[positionInInput + 3].Equals('c') && inputChars[positionInInput + 4].Equals('t') && inputChars[positionInInput + 5].Equals('i') && inputChars[positionInInput + 6].Equals('o') && inputChars[positionInInput + 7].Equals('n') && inputChars[positionInInput + 8].Equals(' '))
                {
                    //Add a new Function token
                    tokensFromInput.Add(new Token(TokenType.FunctionDeclaration, "function", lineIndex));
                    positionInInput += 8;
                }

                //If 'use' keyword is detected
                else if (inputChars[positionInInput].Equals('u') && inputChars[positionInInput + 1].Equals('s') && inputChars[positionInInput + 2].Equals('e') && inputChars[positionInInput + 3].Equals(' '))
                {
                    //Add a new UseKeyword token
                    tokensFromInput.Add(new Token(TokenType.UseKeyword, "use", lineIndex));
                    positionInInput += 3;
                }

                //If 'external' keyword is detected
                else if (inputChars[positionInInput].Equals('e') && inputChars[positionInInput + 1].Equals('x') && inputChars[positionInInput + 2].Equals('t') && inputChars[positionInInput + 3].Equals('e') && inputChars[positionInInput + 4].Equals('r') && inputChars[positionInInput + 5].Equals('n') && inputChars[positionInInput + 6].Equals('a') && inputChars[positionInInput + 7].Equals('l') && inputChars[positionInInput + 8].Equals((char)46))
                {
                    //Add a new External token
                    tokensFromInput.Add(new Token(TokenType.ExternalKeyword, "external", lineIndex));
                    positionInInput += 8;
                }

                //If 'exclusive' keyword is detected
                else if (inputChars[positionInInput].Equals('e') &&
#region
                inputChars[positionInInput + 1].Equals('x') && 
                inputChars[positionInInput + 2].Equals('c') && 
                inputChars[positionInInput + 3].Equals('l') && 
                inputChars[positionInInput + 4].Equals('u') && 
                inputChars[positionInInput + 5].Equals('s') && 
                inputChars[positionInInput + 6].Equals('i') && 
                inputChars[positionInInput + 7].Equals('v') && 
                inputChars[positionInInput + 8].Equals('e') && 
                inputChars[positionInInput + 9].Equals(' '))
                {
                    //Add a new Function token
                    tokensFromInput.Add(new Token(TokenType.ExclusiveKeyword, "exclusive", lineIndex));
                    positionInInput += 9;
                }
#endregion                
                //If 'if' keyword is detected
                else if (inputChars[positionInInput].Equals('i') && inputChars[positionInInput + 1].Equals('f') && inputChars[positionInInput + 2].Equals(' '))
                {
                    //Add a new IfKeyword token
                    tokensFromInput.Add(new Token(TokenType.IfKeyword, "if", lineIndex));
                    positionInInput += 2;
                }

                //If 'for' keyword is detected
                else if (inputChars[positionInInput].Equals('f') && inputChars[positionInInput + 1].Equals('o') && inputChars[positionInInput + 2].Equals('r') && inputChars[positionInInput + 3].Equals(' '))
                {
                    //Add a new ForKeyword token
                    tokensFromInput.Add(new Token(TokenType.ForKeyword, "for", lineIndex));
                    positionInInput += 3;
                }

                //If 'return' keyword is detected
                else if (inputChars[positionInInput].Equals('r') &&
#region
                inputChars[positionInInput + 1].Equals('e') &&
                inputChars[positionInInput + 2].Equals('t') &&
                inputChars[positionInInput + 3].Equals('u') &&
                inputChars[positionInInput + 4].Equals('r') &&
                inputChars[positionInInput + 5].Equals('n') &&
                inputChars[positionInInput + 6].Equals(' '))
                {
                    //Add a new ReturnKeyword token
                    tokensFromInput.Add(new Token(TokenType.ReturnKeyword, "return", lineIndex));
                    positionInInput += 6;
                }
#endregion
                //If identifier is detected (letter)
                else if (Char.IsLetter(inputChars[positionInInput]) || inputChars[positionInInput].Equals('_'))
                {
                    tokensFromInput.Add(CheckForIdentifier());
                }

                //If start of mathematical or comparison operator or equals is detected
                else if (
#region
                    inputChars[positionInInput].Equals((char)43) || //+
                    inputChars[positionInInput].Equals((char)45) || //-
                    inputChars[positionInInput].Equals((char)42) || //*
                    inputChars[positionInInput].Equals((char)47) || // /
                    inputChars[positionInInput].Equals((char)61) || //=
                    inputChars[positionInInput].Equals((char)62) || //>
                    inputChars[positionInInput].Equals((char)60) || //<
                    inputChars[positionInInput].Equals((char)33) || //!
                    inputChars[positionInInput].Equals((char)38) || //&
                    inputChars[positionInInput].Equals((char)124))  //|
                {
                    //Add a new token based on result of CheckForOperatorOrEquals
                    tokensFromInput.Add(CheckForOperatorOrEquals());
                }
#endregion
                //If parenthesis, square bracket, or curly brace is detected
                else if (
#region
                    inputChars[positionInInput].Equals((char)40) || //(
                    inputChars[positionInInput].Equals((char)41) || //)
                    inputChars[positionInInput].Equals((char)91) || //[
                    inputChars[positionInInput].Equals((char)93) || //]
                    inputChars[positionInInput].Equals((char)123)|| //{
                    inputChars[positionInInput].Equals((char)125))  //}
                {
                    //Add new token based on result of CheckForParenthesisBracketBrace
                    tokensFromInput.Add(CheckForParenthesisBracketBrace());
                }
                #endregion
                //If period is detected
                else if (inputChars[positionInInput].Equals((char)46))
                {
                    //Add a new Period token
                    tokensFromInput.Add(new Token(TokenType.Period, ".", lineIndex));
                    positionInInput += 1;
                }

                //If comma is detected
                else if (inputChars[positionInInput].Equals((char)44))
                {
                    //Add a new Comma token
                    tokensFromInput.Add(new Token(TokenType.Comma, ",", lineIndex));
                    positionInInput += 1;
                }

                //If line end is detected (semicolon)
                else if (inputChars[positionInInput].Equals((char)59))
                {
                    //Adds new line ending token
                    tokensFromInput.Add(new Token(TokenType.LineEnding, ";", lineIndex));

                    //Adds one to position and line index
                    positionInInput += 1;
                    lineIndex += 1;
                }

                //If no token can be identified, go to next character (i.e. space)
                else
                {
                    positionInInput += 1;
                }
            }

            return tokensFromInput;
        }

        Token CheckForComment()
        {
            Token newToken = new Token();

            newToken.isOnLine = lineIndex;

            int position = positionInInput;

            //While current position is less than or equal to the length of the input
            while (position < input.Length)
            {
                //Current character
                char character = inputChars[position];

                //If character is a semicolon
                if (inputChars[position].Equals((char)59))
                {
                    //Break out of the loop
                    break;
                }

                //Add character to new token
                newToken.value += character;

                //Go to next char
                position += 1;
            }

            //Update global position to include changes to local position
            positionInInput += newToken.value.Length;

            //Return new token of type Identifier
            newToken.type = TokenType.Comment;
            return newToken;
        }

        Token CheckForIdentifier()
        {
            Token newIdentifierToken = new Token();

            newIdentifierToken.isOnLine = lineIndex;

            int position = positionInInput;

            //While current position is less than or equal to the length of the input
            while (position < input.Length)
            {
                //Current character
                char character = inputChars[position];

                //If character isn't a valid identifier type
                if (!Char.IsLetterOrDigit(character) && !character.Equals('_'))
                {
                    //Break out of the loop
                    break;
                }

                //Add character to new token
                newIdentifierToken.value += character;

                //Go to next char
                position += 1;
            }

            //Update global position to include changes to local position
            positionInInput += newIdentifierToken.value.Length;

            //Return new token of type Identifier
            newIdentifierToken.type = TokenType.Identifier;
            return newIdentifierToken;
        }

        Token CheckForString()
        {
            Token newStringToken = new Token();

            newStringToken.isOnLine = lineIndex;

            int position = positionInInput;

            //Advance position beyond first single quote
            position += 1;

            //While current position is less than or equal to the length of the input
            while (position < input.Length)
            {
                //Current character
                char character = inputChars[position];

                //If character ends the string
                if (character.Equals((char)8.217) || character.Equals((char)39))
                {
                    //Go to next char
                    position += 1;

                    //Break out of the loop
                    break;
                }

                //Add character to new token
                newStringToken.value += character;

                //Go to next char
                position += 1;
            }

            //Update global position to include changes to local position + 2 because of quotes
            positionInInput += (newStringToken.value.Length + 2);

            //Return new token of type StringVariable
            newStringToken.type = TokenType.StringVariable;
            return newStringToken;
        }

        Token CheckForIntOrFloat()
        {
            Token newToken = new Token();

            newToken.isOnLine = lineIndex;

            bool isFloat = false;

            int position = positionInInput;

            //While current position is less than or equal to the length of the input
            while (position < input.Length)
            {
                //Current character
                char character = inputChars[position];

                //If character is a decimal point and doesn't already have one and the next character is a digit
                if (character.Equals((char) 46) && Char.IsDigit(inputChars[position + 1]))
                {
                    //The new token is a float
                    isFloat = true;
                    newToken.type = TokenType.FloatVariable;
                }

                //If character is not a digit and the token isn't a float
                else if (!Char.IsDigit(character) && !isFloat)
                {
                    newToken.type = TokenType.IntVariable;
                    break;
                }

                //If character is not a digit and the token is a float
                else if (!Char.IsDigit(character) && isFloat)
                {
                    break;
                }

                //Add character to new token
                newToken.value += character;

                //Go to next char
                position += 1;
            }

            //Update global position to include changes to local position
            positionInInput += newToken.value.Length;

            //Return new token
            return newToken;
        }

        Token CheckForOperatorOrEquals()
        {
            Token newToken = new Token();

            newToken.isOnLine = lineIndex;

            int position = positionInInput;

            //While current position is less than or equal to the length of the input
            while (position < input.Length)
            {
                //Current character
                char character = inputChars[position];

                //If character is a mathematical operator
                if (character.Equals((char)43) || character.Equals((char)45) || character.Equals((char)42) || character.Equals((char)47))  //+, -, *, or /
                {
                    //Character is the value of the new MathematicalOperator token
                    newToken.value += character;
                    newToken.type = TokenType.MathematicalOperator;

                    //Add one to the position
                    position += 1;

                    //Break out of the loop
                    break;
                }

                //If character is a greater-than or less-than symbol
                else if (character.Equals((char)62) || character.Equals((char)60))   //>, <, also includes checks for >=, <=
                {
                    //If the following character is an equals
                    if (inputChars[position + 1].Equals((char)61))
                    {
                        //Character + the equals is the value of the new ComparisonOperator token
                        newToken.value += character;
                        newToken.value += inputChars[position + 1];
                        newToken.type = TokenType.ComparisonOperator;

                        //Add one to the position
                        position += 1;

                        //Break out of the loop
                        break;
                    }

                    //If the following character is not an equals
                    else
                    {
                        //Character is the value of the new ComparisonOperator token
                        newToken.value += character;
                        newToken.type = TokenType.ComparisonOperator;

                        //Add one to the position
                        position += 1;

                        //Break out of the loop
                        break;
                    }
                }

                //If character is an exclamation, &, or |
                else if (character.Equals((char)33) || character.Equals((char)38) || character.Equals((char)124))
                {
                    //Check if exclamation mark doesn't have equals after it
                    if (character.Equals((char)33) && !inputChars[position+1].Equals((char)61))
                    {
                        //If it doesn't, throw a compiler error
                        CrossoverCompiler.ThrowCompilerError("Exclamation mark (!) must have equals (=) immediately following it.", lineIndex);
                    }

                    //Check if exclamation mark has equals after it
                    else if (character.Equals((char)33) && inputChars[position + 1].Equals((char)61))
                    {
                        //If it does, return a token with !=
                        newToken.value += character;
                        newToken.value += inputChars[position + 1];
                        newToken.type = TokenType.ComparisonOperator;

                        //Add two to the position
                        position += 2;

                        //Break out of the loop
                        break;
                    }

                    //If character doesn't meet either of the above if-checks (it's & or |)

                    //Character is the value of the new ComparisonOperator token
                    newToken.value += character;
                    newToken.type = TokenType.ComparisonOperator;

                    //Add one to the position
                    position += 1;

                    //Break out of the loop
                    break;
                }

                //If character is an equals
                else if (character.Equals((char)61))
                {
                    //Character is the value of the new Equals token
                    newToken.value += character;
                    newToken.type = TokenType.Equals;

                    //Add one to the position
                    position += 1;

                    //Break out of the loop
                    break;
                }
            }

            //Update global position to include changes to local position
            positionInInput += newToken.value.Length;

            return newToken;
        }

        Token CheckForParenthesisBracketBrace()
        {
            Token newToken = new Token();

            newToken.isOnLine = lineIndex;

            int position = positionInInput;

            //While current position is less than or equal to the length of the input
            while (position < input.Length)
            {
                //Current character
                char character = inputChars[position];

                //If character is a left or right parenthesis
                if (character.Equals((char)40) || character.Equals((char)41))
                {
                    //Character is the value of the new Parenthesis token
                    newToken.value += character;
                    newToken.type = TokenType.Parenthesis;

                    //Add one to the position
                    position += 1;

                    //Break out of the loop
                    break;
                }

                //If character is a left or right square bracket
                else if (character.Equals((char)91) || character.Equals((char)93))
                {
                    //Character is the value of the new Bracket token
                    newToken.value += character;
                    newToken.type = TokenType.SquareBracket;

                    //Add one to the position
                    position += 1;

                    //Break out of the loop
                    break;
                }

                //If character is a left or right curly brace
                else if (character.Equals((char)123) || character.Equals((char)125))
                {
                    //Character is the value of the new Brace token
                    newToken.value += character;
                    newToken.type = TokenType.CurlyBrace;

                    //Add one to the position
                    position += 1;

                    //Break out of the loop
                    break;
                }
            }

            //Update global position to include changes to local position
            positionInInput += newToken.value.Length;

            return newToken;
        }
    }

    public class Token
    {
        public TokenType type;
        public string value;
        public int isOnLine;

        public Token()
        {
            //Empty constructor
        }

        public Token(TokenType _type, string _value, int _lineIndex)
        {
            type = _type;
            value = _value;
            isOnLine = _lineIndex;
        }

        public Token(TokenType _type, bool _value, int _lineIndex)
        {
            type = _type;
            value = _value.ToString();
            isOnLine = _lineIndex;
        }

        public Token(TokenType _type, int _value, int _lineIndex)
        {
            type = _type;
            value = _value.ToString();
            isOnLine = _lineIndex;
        }

        public Token(TokenType _type, float _value, int _lineIndex)
        {
            type = _type;
            value = _value.ToString();
            isOnLine = _lineIndex;
        }
    }

    public enum TokenType
    {
        Comment,            // '//'
        VariableDeclaration, //var
        StringVariable,     //' to '
        BoolVariable,       //letter AND true, false
        IntVariable,        //digit, NO decimal
        FloatVariable,      //digit, decimal
        UseKeyword,         //use
        ExternalKeyword,
        FunctionDeclaration, //function
        ExclusiveKeyword,   //exclusive
        IfKeyword,          //if
        ForKeyword,         //for
        ReturnKeyword,      //return
        Identifier,         //MUST START WITH letter, CAN CONTAIN digit, _ (underscore)
        Equals,             //=
        MathematicalOperator, //+, -, *, /
        ComparisonOperator, //>, <, ==, !=, >=, <=, &, |
        Parenthesis,        //(, )
        CurlyBrace,         //{, }
        SquareBracket,      //[, ]
        Comma,              //,
        Period,             //.
        LineEnding          //;
    }
}