using Crossover;
using System.Collections.Generic;

namespace Crossover
{
    class UtilityTools
    {
        //Use to convert a list of tokens into a string
        public static string TokenListToString(List<Token> tokenListToConvert)
        {
            string stringToReturn = string.Empty;

            //Foreach token passed in the token list
            foreach (Token tkn in tokenListToConvert)
            {
                //Add the token's value to the string
                stringToReturn += tkn.value;
            }

            //Return the completed string
            return stringToReturn;
        }
    }
}
