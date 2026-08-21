namespace KeySwitcher.Services;

public static class LayoutConverter
{
    private const string English = "`qwertyuiop[]asdfghjkl;'zxcvbnm,.~QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>";
    private const string Russian = "ёйцукенгшщзхъфывапролджэячсмитьбюЁЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ";
    private static readonly HashSet<string> CommonRu = new(StringComparer.OrdinalIgnoreCase) { "и", "в", "не", "на", "что", "это", "как", "для", "привет", "спасибо", "пожалуйста", "можно", "нужно", "сейчас", "текст", "звук", "проект", "дорожка" };
    private static readonly HashSet<string> CommonEn = new(StringComparer.OrdinalIgnoreCase) { "the", "and", "you", "that", "this", "for", "with", "hello", "thanks", "please", "what", "when", "where", "reaper", "windows", "audio", "sound", "plugin", "track", "project" };

    public static string Convert(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var p = English.IndexOf(chars[i]);
            if (p >= 0) chars[i] = Russian[p];
            else if ((p = Russian.IndexOf(chars[i])) >= 0) chars[i] = English[p];
        }
        return new string(chars);
    }

    public static bool IsWrong(string word)
    {
        if (word.Length < 2) return false;
        var converted = Convert(word);
        if (CommonRu.Contains(converted) || CommonEn.Contains(converted)) return true;
        var latin = word.Any(c => c is >= 'A' and <= 'z');
        var sourceScore = Score(word, latin);
        var targetScore = Score(converted, !latin);
        return targetScore >= 4 && targetScore >= sourceScore + 4;
    }

    private static int Score(string word, bool english)
    {
        if ((english ? CommonEn : CommonRu).Contains(word)) return 12;
        var pairs = english ? "th he in er an re on at en nd ti es or te ed is it al ar st ng ch" : "ст но то на ен ов ни ра во ко ро пр по ос ал го ер от та ор ол ан ре ит";
        var score = 0;
        for (var i = 1; i < word.Length; i++) if (pairs.Contains(word.Substring(i - 1, 2), StringComparison.OrdinalIgnoreCase)) score += 2;
        return score;
    }
}
