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
            //If 'var' is detected
            if (inputChars[positionInInput].Equals('v'))
            {
                if (inputChars[positionInInput+1].Equals('a'))
                    if (inputChars[positionInInput + 2].Equals('r'))
                        if (inputChars[positionInInput + 3].Equals(' '))
                        {
                            //Add a new Variable Declaration token
                            tokensFromInput.Add(new Token(TokenType.VariableDeclaration), "var");
                            positionInInput += 1;
                        }
            }

            //Check if character can be or is identifier
            else if (Char.IsLetter(inputChars[positionInInput]))
            {
                tokensFromInput.Add(CheckForIdentifier());
            }

            //If no token can be identified, go to next character
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
    VariableDeclaration, //var
    StringVariable,     //" to "
    BoolVariable,       //letter AND true, false
    IntVariable,        //digit, NO decimal
    FloatVariable,      //digit, decimal
    Identifier,         //MUST START WITH letter, CAN CONTAIN digit, _ (underscore)
    Function,           //
    Operator            //+, -, *, /, =, ==, !=, >=, <=
}