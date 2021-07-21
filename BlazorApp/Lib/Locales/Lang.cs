using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp.Lib.Locales
{
    public static class Langs
    {
        public static Dictionary<Language, string> SubmitButton = new Dictionary<Language, string>();
        public static Dictionary<Language, string> CancelButton = new Dictionary<Language, string>();
        public static Dictionary<Language, string> WelcomeText1 = new Dictionary<Language, string>();
        public static Dictionary<Language, string> WelcomeText2 = new Dictionary<Language, string>();
        public static Dictionary<Language, string> WelcomeText3 = new Dictionary<Language, string>();
        public static Dictionary<Language, string> TermsText = new Dictionary<Language, string>();
        public static Dictionary<Language, string> Help1 = new Dictionary<Language, string>();
        public static Dictionary<Language, string> Help2 = new Dictionary<Language, string>();
        public static Dictionary<Language, string> Help3 = new Dictionary<Language, string>();


    }

    public enum Language
    {
        EN,
        FR,
        DE,
        ES
    }
}
