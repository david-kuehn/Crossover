using System;
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
                if (inputChars[positionInInput].Equals((char)47) && !inputChars[positionInInput].Equals((char)102))
                {
                    if (inputChars[positionInInput + 1].Equals((char)47))
                        tokensFromInput.Add(CheckForComment());
                }

                //If variable declaration is detected ('var')
                else if (inputChars[positionInInput].Equals('v') && inputChars[positionInInput + 1].Equals('a') && inputChars[positionInInput + 2].Equals('r') && inputChars[positionInInput + 3].Equals(' '))
                {
                    //Add a new Variable Declaration token
                    tokensFromInput.Add(new Token(TokenType.VariableDeclaration, "var"));
                    positionInInput += 3;
                }

                //If string is detected (single quote) using Unicode for single quote/apostrophe
                else if (inputChars[positionInInput].Equals((char)8.217) || inputChars[positionInInput].Equals((char)39))
                {
                    tokensFromInput.Add(CheckForString());
                }

                //If 'true' is detected (bool variable)
                else if (inputChars[positionInInput].Equals('t') && inputChars[positionInInput+1].Equals('r') && inputChars[positionInInput + 2].Equals('u') && inputChars[positionInInput + 3].Equals('e') && !Char.IsLetterOrDigit(inputChars[positionInInput + 4]))
                {
                    tokensFromInput.Add(new Token(TokenType.BoolVariable, "true"));

                    //Relocates position to character after 'true' word
                    positionInInput += 4;
                }
                  
                //If 'false' is detected (bool variable)
                else if (inputChars[positionInInput].Equals('f') && inputChars[positionInInput + 1].Equals('a') && inputChars[positionInInput + 2].Equals('l') && inputChars[positionInInput + 3].Equals('s') && inputChars[positionInInput + 4].Equals('e') && !Char.IsLetterOrDigit(inputChars[positionInInput + 5]))
                {
                    tokensFromInput.Add(new Token(TokenType.BoolVariable, "false"));

                    //Relocates position to character after 'false' word
                    positionInInput += 5;
                }

                //If function declaration is detected
                else if (inputChars[positionInInput].Equals('f') && inputChars[positionInInput + 1].Equals('u') && inputChars[positionInInput + 2].Equals('n') && inputChars[positionInInput + 3].Equals('c') && inputChars[positionInInput + 4].Equals('t') && inputChars[positionInInput + 5].Equals('i') && inputChars[positionInInput + 6].Equals('o') && inputChars[positionInInput + 7].Equals('n') && inputChars[positionInInput + 8].Equals(' '))
                {
                    //Add a new Function token
                    tokensFromInput.Add(new Token(TokenType.FunctionDeclaration, "function"));
                    positionInInput += 8;
                }

                //If identifier is detected (letter)
                else if (Char.IsLetter(inputChars[positionInInput]) || inputChars[positionInInput].Equals('_'))
                {
                    tokensFromInput.Add(CheckForIdentifier());
                }

                //If int or float is detected
                else if (Char.IsDigit(inputChars[positionInInput]))
                {
                    tokensFromInput.Add(CheckForIntOrFloat());
                }

                //If line end is detected (semicolon)
                else if (inputChars[positionInInput].Equals((char)59))
                {
                    //Adds new line ending token
                    tokensFromInput.Add(new Token(TokenType.LineEnding, ";"));

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
            Token newIdentifierToken = new Token();

            int position = positionInInput;

            //Add first single quote to new token and advance position
            newIdentifierToken.value += inputChars[position];
            position += 1;

            //While current position is less than or equal to the length of the input
            while (position < input.Length)
            {
                //Current character
                char character = inputChars[position];

                //If character ends the string
                if (character.Equals((char)8.217) || character.Equals((char)39))
                {
                    //Add character to new token
                    newIdentifierToken.value += character;

                    //Go to next char
                    position += 1;

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

            //Return new token of type StringVariable
            newIdentifierToken.type = TokenType.StringVariable;
            return newIdentifierToken;
        }

        Token CheckForIntOrFloat()
        {
            Token newToken = new Token();

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
    }

    public class Token
    {
        public TokenType type;
        public string value;

        public Token()
        {

        }

        public Token(TokenType _type, string _value)
        {
            type = _type;
            value = _value;
        }

        public Token(TokenType _type, bool _value)
        {
            type = _type;
            value = _value.ToString();
        }

        public Token(TokenType _type, int _value)
        {
            type = _type;
            value = _value.ToString();
        }

        public Token(TokenType _type, float _value)
        {
            type = _type;
            value = _value.ToString();
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
        Identifier,         //MUST START WITH letter, CAN CONTAIN digit, _ (underscore)
        FunctionDeclaration, //function
        ExclusiveKeyword,   //exclusive
        PrintKeyword,       //print
        MathematicalOperator, //+, -, *, /, =
        ComparisonOperator, //>, <, ==, !=, >=, <=, and, or
        Parenthesis,        //(, )
        CurlyBrace,         //{, }
        SquareBracket,      //[, ]
        LineEnding          //;
    }
}