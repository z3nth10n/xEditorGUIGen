using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using xEditorGUIGen.Properties;

namespace xEditorGUIGen
{
    internal class Program
    {
        private static string[] styleNames = new[] { "boldFont", "boldLabel", "centeredGreyMiniLabel", "colorField", "foldout", "foldoutPreDrop", "helpBox", "inspectorDefaultMargins", "inspectorFullWidthMargins", "label", "largeLabel", "layerMaskField", "miniBoldFont", "miniBoldLabel", "miniButton", "miniButtonLeft", "miniButtonMid", "miniButtonRight", "miniFont", "miniLabel", "miniTextField", "numberField", "objectField", "objectFieldMiniThumb", "objectFieldThumb", "popup", "radioButton", "standardFont", "textArea", "textField", "toggle", "toggleGroup", "toolbar", "toolbarButton", "toolbarDropDown", "toolbarPopup", "toolbarTextField", "whiteBoldLabel", "whiteLabel", "whiteLargeLabel", "whiteMiniLabel", "wordWrappedLabel", "wordWrappedMiniLabel" };

        private static void Main(string[] args)
        {
            //Console.WriteLine(Resources.EditorGUI);

            Regex ItemRegex = new Regex(@"public static(?<MethodName>.+?)\((?<Params>.+?)\)(?<=([^;]))+$", RegexOptions.Multiline | RegexOptions.Compiled);
            MatchCollection matches = ItemRegex.Matches(Resources.EditorGUI);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.Internal;");
            sb.AppendLine("using UnityEngine.Rendering;");
            sb.AppendLine("using UnityEditor;");
            sb.AppendLine("using Object = UnityEngine.Object;");
            sb.AppendLine();

            sb.AppendLine("namespace z3nth10n.EditorUtils");
            sb.AppendLine("{");

            sb.AppendLineIndented(@"
                                    public static class FEditor
                                    {
                                        public static void AddOrSet<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
                                        {
                                            if (!dictionary.ContainsKey(key))
                                                dictionary.Add(key, value);
                                            else
                                                dictionary[key] = value;
                                        }
                                    }", 1);

            sb.AppendLine();

            sb.AppendLineIndented(@"
                                    public class StyleWrapper
                                    {
                                        public Dictionary<GUIStyle, GUIContents> listWrapper = new Dictionary<GUIStyle, GUIContents>();

                                        public StyleWrapper(GUIStyle style, GUIContent label)
                                        {
                                            listWrapper.AddOrSet(style, GUIContents.AddOrSet(GUIContents.GetContents(style.name), label));
                                        }

                                        public StyleWrapper(GUIStyle style, string label)
                                        {
                                            listWrapper.AddOrSet(style, GUIContents.AddOrSet(GUIContents.GetContents(style.name), new GUIContent(label)));
                                        }
                                    }

                                    public class GUIContents
                                    {
                                        private static Dictionary<string, GUIContents> dictionary = new Dictionary<string, GUIContents>();

                                        public List<GUIContent> list = new List<GUIContent>();

                                        public static GUIContents GetContents(string name)
                                        {
                                            if (!dictionary.ContainsKey(name))
                                                dictionary.Add(name, new GUIContents());

                                            return dictionary[name];
                                        }

                                        public static GUIContents AddOrSet(GUIContents contents, GUIContent content)
                                        {
                                            int index = contents.list.IndexOf(content);

                                            if (index == -1)
                                                contents.list.Add(content);
                                            else
                                                contents.list[index] = content;

                                            return contents;
                                        }
                                    }", 1);

            sb.AppendLine();

            sb.AppendLineIndented("public class xEditorGUI", 1);
            sb.AppendLineIndented("{", 1);

            sb.AppendLineIndented("private Dictionary<string, StyleWrapper> stylesDict = new Dictionary<string, StyleWrapper>();", 2);
            sb.AppendLine();

            foreach (Match ItemMatch in matches)
            {
                string methodDef = ItemMatch.Value.Replace(" static", "");

                sb.AppendLineIndented($"{methodDef}", 2);
                sb.AppendLineIndented("{", 2);

                string[] parsArr = null;
                string[][] parsPreObj = null;

                string replacedGeneric = "";

                string[] methodWithType = ItemMatch.Groups["MethodName"].Value.Trim().Split(' ');

                try
                {
                    string Params = ItemMatch.Groups["Params"].Value;
                    // int equalsPos = Params.IndexOf('=');

                    // ReplaceByPosition(" = ", equalsPos, Params.IndexOf(',', equalsPos + 1) - equalsPos - 1)

                    // string _Params = (string)Params.Clone();

                    //bool hasOptionalParams = Params.Contains('='),
                    //     hasOptionalAttributeParams = hasOptionalParams && Params.Contains(")]");

                    bool hasAttributeParams = Params.Contains(")]");

                    if (hasAttributeParams)
                    {
                        sb.AppendLineIndented(@"throw new Exception(""Methods with attributed params aren't implemented yet!"");", 3);

                        sb.AppendLineIndented("}", 2);
                        sb.AppendLine();

                        continue;
                    }

                    if (Params.Contains('='))
                        Params = new Regex(" =.+?,").Replace(Params, ",");

                    if (Params.Contains('='))
                        Params = new Regex(" =.+?$").Replace(Params, "");

                    replacedGeneric = Params.ReplaceIf(", ", ",", '<');
                    parsArr = replacedGeneric.Split(new string[] { ", " }, StringSplitOptions.None).ToArray();

                    parsPreObj = parsArr
                        .Select(x => x.Trim().ReplaceIf("] ", "]", ")]").Split(' '))
                        .ToArray();

                    IEnumerable<Param> parsObjects = parsPreObj.Select(x => x.Length == 2 ? new Param(x.First(), x[1]) : new Param(x[0], x[1], x[2]));

                    if (parsObjects.Any(x => x.type == "Editor"))
                    {
                        sb.AppendLineIndented(@"throw new Exception(""Methods with Editor type in it's parameters aren't implemented yet!"");", 3);

                        sb.AppendLineIndented("}", 2);
                        sb.AppendLine();

                        continue;
                    }

                    bool hasNoPosition = !parsObjects.Any(x => x.type == "Rect" && x.name == "position");

                    IEnumerable<string> parsNames = parsObjects.Select(x => x.name);

                    string contentParam = parsNames.GetParam();

                    bool hasStyle = parsNames.Contains("style");

                    string pars = parsObjects.GetParams();

                    string styleName = methodWithType[1].FirstCharToLower();

                    bool styleExists = styleNames.Contains(styleName),
                         addBody = hasStyle || styleExists,
                         hasNoContent = string.IsNullOrEmpty(contentParam);

                    if (hasNoContent)
                        contentParam = "GUIContent.none";

                    if (hasNoPosition)
                        sb.AppendLineIndented(@"throw new Exception(""No implementation Layout yet!"");", 3);
                    else
                    {
                        if (addBody)
                            sb.AppendLineIndented($@"if(!stylesDict.ContainsKey(""{methodWithType[1]}""))", 3);

                        if (hasStyle)
                            sb.AppendLineIndented($@"stylesDict.Add(""{methodWithType[1]}"", new StyleWrapper(style, {contentParam}));", 4);
                        else if (styleExists)
                            sb.AppendLineIndented($@"stylesDict.Add(""{methodWithType[1]}"", new StyleWrapper(EditorStyles.{styleName}, {contentParam}));", 4);

                        if (addBody)
                            sb.AppendLine();

                        sb.AppendLineIndented($"{(methodWithType[0] == "void" ? "" : "return ")}EditorGUI.{methodWithType[1]}({pars});", 3);
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLineIndented($"// Couldn't add this line! (Exception: {ex.GetType().FullName} -- At line: {ex.LineNumber()})", 3);

                    if (methodWithType[0] != "void")
                        sb.AppendLineIndented("throw new Exception();", 3);
                }

                sb.AppendLineIndented("}", 2);
                sb.AppendLine();
            }

            sb.AppendLineIndented(@"
                                    public float GetHeight()
                                    {
                                        float size = 0;

                                        foreach (StyleWrapper wrapper in stylesDict.Values)
                                            size += wrapper.listWrapper.Sum(x => x.Value.list.Sum(y => x.Key.CalcSize(y).y));

                                        return size;
                                    }", 2);

            sb.AppendLineIndented("}", 1);

            sb.AppendLine("}");

            string codeGen = sb.ToString();

            codeGen.CopyToClipboard();

            Console.WriteLine($"{codeGen.Length} characters copied to clipboard.");

            Console.Read();
        }
    }

    internal static class F
    {
        public static string ReplaceIf(this string str, string find, string replacement, string match)
        {
            return str.Contains(match) ? str.Replace(find, replacement) : str;
        }

        public static string ReplaceIf(this string str, string find, string replacement, char match)
        {
            if (str.Contains(match))
            {
                int indexOf = str.IndexOf(find, str.IndexOf(match)),
                    len = find.Length;

                string s = str.ReplaceByPosition(replacement, indexOf, len);

                return s;
            }
            else
                return str;
        }

        public static string FirstCharToLower(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToLower() + input.Substring(1);
            }
        }

        public static void AppendLineIndented(this StringBuilder stringBuilder, string line, int tabs)
        {
            stringBuilder.AppendLine(new string('\t', tabs) + line);
        }

        public static void CopyToClipboard(this string data)
        {
            Thread thread = new Thread(() => Clipboard.SetText(data));
            thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            thread.Start();
            thread.Join();
        }

        public static string GetParam(this IEnumerable<string> pars)
        {
            if (pars.Contains("label"))
                return "label";
            else if (pars.Contains("content"))
                return "content";
            else if (pars.Contains("message"))
                return "message";
            else if (pars.Contains("text"))
                return "text";
            else
                return "";
        }

        public static string GetParams(this IEnumerable<Param> values)
        {
            return string.Join(", ", values.Select(x => GetParam(x)));
        }

        private static string GetParam(Param par)
        {
            return GetModifier(par.modifier) + par.name;
        }

        private static string GetModifier(string modifier)
        {
            if (modifier == "out" || modifier == "ref")
                return modifier + " ";

            return "";
        }

        public static string ReplaceByPosition(this string str, string replaceBy, int offset, int count)
        {
            if (!str.Contains(replaceBy))
                return str;

            return new StringInfo(str).ReplaceByPosition(replaceBy, offset, count).String;
        }

        public static StringInfo ReplaceByPosition(this StringInfo str, string replaceBy, int offset, int count)
        {
            if (!str.String.Contains(replaceBy))
                return str;

            return str.RemoveByTextElements(offset, count).InsertByTextElements(offset, replaceBy);
        }

        public static StringInfo RemoveByTextElements(this StringInfo str, int offset, int count)
        {
            return new StringInfo(string.Concat(
                str.SubstringByTextElements(0, offset),
                offset + count < str.LengthInTextElements
                    ? str.SubstringByTextElements(offset + count, str.LengthInTextElements - count - offset)
                    : ""
                ));
        }

        public static StringInfo InsertByTextElements(this StringInfo str, int offset, string insertStr)
        {
            if (string.IsNullOrEmpty(str?.String))
                return new StringInfo(insertStr);
            return new StringInfo(string.Concat(
                str.SubstringByTextElements(0, offset),
                insertStr,
                str.LengthInTextElements - offset > 0 ? str.SubstringByTextElements(offset, str.LengthInTextElements - offset) : ""
            ));
        }

        public static int LineNumber(this Exception e)
        {
            int linenum = 0;
            try
            {
                //linenum = Convert.ToInt32(e.StackTrace.Substring(e.StackTrace.LastIndexOf(":line") + 5));

                //For Localized Visual Studio ... In other languages stack trace  doesn't end with ":Line 12"
                linenum = Convert.ToInt32(e.StackTrace.Substring(e.StackTrace.LastIndexOf(' ')));
            }
            catch
            {
                //Stack trace is not available!
            }
            return linenum;
        }
    }

    public class Param
    {
        public string modifier = "",
                      type = "",
                      name = "";

        public Param(string type, string name)
        {
            this.type = type;
            this.name = name;
        }

        public Param(string modifier, string type, string name)
        {
            this.modifier = modifier;
            this.type = type;
            this.name = name;
        }
    }
}