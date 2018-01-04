using System;
using System.Collections.Generic;

public class Tokenizer 
{
    string input;
    char[] inputChars = input.ToCharArray();

    int positionInInput;

    //Returns a list of tokens from the input
    public List<Token> InputToTokensList(string input)
    {
        List<Token> tokensFromInput;
        
        //Handle token checks while current position in input is less than the total length
        while (positionInInput <= inputChars.Length)
        {
            //If variable declaration is detected ('var')
            if (inputChars[positionInInput].Equals('v'))
            {
                if (inputChars[positionInInput + 1].Equals('a'))
                    if (inputChars[positionInInput + 2].Equals('r'))
                        if (inputChars[positionInInput + 3].Equals(' '))
                        {
                            //Add a new Variable Declaration token
                            tokensFromInput.Add(new Token(TokenType.VariableDeclaration), "var");
                            positionInInput += 3;
                        }
            }

            //If string declaration is detected (single/double quote)
            else if (inputChars[positionInInput].Equals("'"))
            {
                tokensFromInput.Add(CheckForString());
            }

            //If bool declaration is detected ('true', 'false')
            else if (inputChars[positionInInput].Equals('t') || inputChars[positionInInput].Equals('f'))
            {
                //If the character is a 't'
                if (inputChars[positionInInput].Equals('t'))
                {
                    string checkTrue;
                    checkTrue = inputChars[positionInInput];

                    //For the next three characters in the input
                    for (int i = 0; i < 3; i++)
                    {
                        //Add them to the checkTrue string
                        checkTrue += inputChars[positionInInput + i];
                    }

                    //If they form the word true AND the character after 'true' isn't a letter/digit (makes sure it isn't identifier)
                    if (checkTrue == "true" && !Char.isLetterOrDigit(inputChars[positionInInput+4]))
                    {
                        tokensFromInput.Add(new Token(TokenType.BoolVariable, "true"));

                        //Relocates position to character after 'true' word
                        positionInInput += 4;
                    }
                }

                else if (inputChars[positionInInput].Equals('f'))
                {
                    string checkFalse;
                    checkFalse = inputChars[positionInInput];

                    //For the next four characters in the input
                    for (int i = 0; i < 4; i++)
                    {
                        //Add them to the checkFalse string
                        checkFalse += inputChars[positionInInput + i];
                    }

                    //If they form the word false AND the character after 'false' isn't a letter/digit (makes sure it isn't identifier)
                    if (checkFalse == "false" && !Char.isLetterOrDigit(inputChars[positionInInput + 5]))
                    {
                        tokensFromInput.Add(new Token(TokenType.BoolVariable, "false"));

                        //Relocates position to character after 'false' word
                        positionInInput += 5;
                    }
                }
            }

            //If identifier is detected (letter)
            else if (Char.IsLetter(inputChars[positionInInput]))
            {
                tokensFromInput.Add(CheckForIdentifier());
            }

            //If no token can be identified, go to next character (i.e. space)
            else
            {
                positionInInput += 1;
            }
        }
        
        return tokensFromInput;
    }

    Token CheckForIdentifier()
    {
        Token newIdentifierToken = new Token();

        int position = positionInInput;

        //While current position is less than or equal to the length of the input
        while (position <= input.Length)
        {
            //Current character
            char character = inputChars[position];

            //If character isn't a valid identifier type
            if (!Char.IsLetterOrDigit(character) || !character.Equals('_'))
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

        //While current position is less than or equal to the length of the input
        while (position <= input.Length)
        {
            //Current character
            char character = inputChars[position];

            //If character ends the string
            if (inputChars[positionInInput].Equals("'"))
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

        //Return new token of type StringVariable
        newIdentifierToken.type = TokenType.StringVariable;
        return newIdentifierToken;
    }

    Tokenizer(string _input)
    {
        input = _input; 
    }
}

public class Token 
{
    public TokenType type; 
    public var value;

    Token (TokenType _type, string _value)
    {
        type = _type; 
        value = _value;
    }

    Token(TokenType _type, bool _value)
    {
        type = _type;
        value = _value;
    }

    Token(TokenType _type, int _value)
    {
        type = _type;
        value = _value;
    }

    Token(TokenType _type, float _value)
    {
        type = _type;
        value = _value;
    }
}

public enum TokenType 
{
    Comment,            // \\
    VariableDeclaration, //var
    StringVariable,     //' to '
    BoolVariable,       //letter AND true, false
    IntVariable,        //digit, NO decimal
    FloatVariable,      //digit, decimal
    Identifier,         //MUST START WITH letter, CAN CONTAIN digit, _ (underscore)
    Function,           //exec
    Operator,           //+, -, *, /, =
    GreaterThan,        //>
    LessThan,           //<
    DblEquals,          //==
    DoesNotEqual,       //!=
    GreaterThanOrEqualTo, //>=
    LessThanOrEqualTo   //<=
}