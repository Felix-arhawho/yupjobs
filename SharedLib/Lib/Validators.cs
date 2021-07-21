using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib.Lib
{
    public static class Validators
    {
        public static bool ValidateBankAccount(string bankAccount)
        {
            return true;
            //bankAccount = bankAccount.ToUpper(); //IN ORDER TO COPE WITH THE REGEX BELOW
            //if (String.IsNullOrEmpty(bankAccount))
            //    return false;
            //else if (System.Text.RegularExpressions.Regex.IsMatch(bankAccount, "^[A-Z0-9]"))
            //{
            //    bankAccount = bankAccount.Replace(" ", String.Empty);
            //    string bank =
            //    bankAccount.Substring(4, bankAccount.Length - 4) + bankAccount.Substring(0, 4);
            //    int asciiShift = 55;
            //    StringBuilder sb = new StringBuilder();
            //    foreach (char c in bank)
            //    {
            //        int v;
            //        if (Char.IsLetter(c)) v = c - asciiShift;
            //        else v = int.Parse(c.ToString());
            //        sb.Append(v);
            //    }
            //    string checkSumString = sb.ToString();
            //    int checksum = int.Parse(checkSumString.Substring(0, 1));
            //    for (int i = 1; i < checkSumString.Length; i++)
            //    {
            //        int v = int.Parse(checkSumString.Substring(i, 1));
            //        checksum *= 10;
            //        checksum += v;
            //        checksum %= 97;
            //    }
            //    return checksum == 1;
            //}
            //else
            //    return false;
        }
    }
}
