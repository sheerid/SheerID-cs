using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTools
{
    public class ConsoleKeySelection
    {
        public bool Selected;
        public bool Ctrl;
        public bool Shift;
        public ConsoleKey Key;
        public string DisplayName;
    }

    public class Output
    {
        public Output(System.Reflection.Assembly targetAssembly)
        {
            this.targetAssembly = targetAssembly;
        }
        private System.Reflection.Assembly targetAssembly;
        public string GetUniqueFilePath(string path)
        {
            if (System.IO.File.Exists(path))
            {
                string dir = System.IO.Path.GetDirectoryName(path);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                string fileExt = System.IO.Path.GetExtension(path);

                var r = new System.Text.RegularExpressions.Regex(@"\[(?<digits>[\d]+)\]" + fileExt + "$");
                var lastfile = System.IO.Directory.GetFiles(dir, fileName + "[*]" + fileExt).OrderBy(o => int.Parse(r.Match(o).Groups["digits"].Value)).LastOrDefault();
                if (lastfile != null)
                {
                    string lastFileName = System.IO.Path.GetFileNameWithoutExtension(lastfile);
                    var m = r.Match(lastFileName + fileExt);
                    if (m.Success)
                    {
                        var c = m.Captures[0];
                        path = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            lastFileName.Substring(0, c.Index) + "[" + (int.Parse(m.Groups["digits"].Value) + 1) + "]" + fileExt
                            );
                    }
                    else
                    {
                        path = System.IO.Path.Combine(dir, lastFileName + "[2]" + fileExt);
                    }
                }
                else
                {
                    path = System.IO.Path.Combine(dir, fileName + "[2]" + fileExt);
                }
            }
            if (System.IO.File.Exists(path))
            {
                throw new Exception("GetUniqueFilePath failed");
            }
            return path;
        }
        public T PromptFor<T>(string prompt)
        {
            Console.WriteLine(prompt);
            Console.Write(">");
            return (T)Convert.ChangeType(Console.ReadLine(), typeof(T));
        }
        public Dictionary<ConsoleKeySelection, T> CreateKeyMatrix<T>(ICollection<T> collection)
        {
            Dictionary<ConsoleKeySelection, T> keyMatrix = new Dictionary<ConsoleKeySelection, T>();
            int i = 0;
            ConsoleKey[] keys = new[] { ConsoleKey.D0, ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.D3, ConsoleKey.D4, ConsoleKey.D5, ConsoleKey.D6, ConsoleKey.D7, ConsoleKey.D8, ConsoleKey.D9 };

            foreach (var aff in collection)
            {
                i++;
                bool ctrl = false;
                bool shft = false;
                var keyIndex = i % keys.Length;
                if (i > keys.Length * 3)
                {
                    ctrl = true;
                    shft = true;
                }
                else if (i > keys.Length * 2)
                {
                    shft = true;
                }
                else if (i > keys.Length)
                {
                    ctrl = true;
                }
                keyMatrix.Add(new ConsoleKeySelection() { Key = keys[keyIndex], Selected = false, Shift = shft, Ctrl = ctrl }, aff);
                if (i >= 22)
                    break;
            }
            return keyMatrix;
        }
        public IEnumerable<KeyValuePair<ConsoleKeySelection, T>> PromptKeyMatrix<T>(Dictionary<ConsoleKeySelection, T> keyMatrix, ICollection<T> collection, string selectionTitle, MatrixSelectionMethod method = MatrixSelectionMethod.SelectOne)
        {
            while (1 == 1)
            {
                Console.WriteLine(selectionTitle + " [Results: " + collection.Count + "]" + (collection.Count > 22 ? " RESULTS TRUNCATED" : "") + ":");
                foreach (var choice in keyMatrix)
                {
                    Console.WriteLine(string.Format(
                        "[{0}] {1}{2}{3}{4}) {5}",
                        choice.Key.Selected ? "*" : " ",
                        choice.Key.Ctrl || choice.Key.Shift ? choice.Key.Ctrl && choice.Key.Shift ? "" : "     " : "          ",
                        choice.Key.Ctrl ? "ctrl+" : "",
                        choice.Key.Shift ? "shft+" : "",
                        choice.Key.Key.ToString().Substring(1),
                        string.IsNullOrEmpty(choice.Key.DisplayName) ? choice.Value.ToString() : choice.Key.DisplayName
                        ));
                }

                Console.WriteLine("Select one " + (method == MatrixSelectionMethod.SelectMany ? "or more " : "") + "from above and ENTER to continue.");
                Console.Write(">");
                var commandKey = Console.ReadKey();
                Console.WriteLine();

                if (commandKey.Key == ConsoleKey.Enter && keyMatrix.Any(o => o.Key.Selected == true))
                    break;
                var selection = keyMatrix.FirstOrDefault(o => o.Key.Key == commandKey.Key && o.Key.Ctrl == commandKey.Modifiers.HasFlag(ConsoleModifiers.Control) && o.Key.Shift == commandKey.Modifiers.HasFlag(ConsoleModifiers.Shift));
                if (!new KeyValuePair<ConsoleKeySelection, T>().Equals(selection))//not default
                {
                    if (method == MatrixSelectionMethod.SelectOne)
                    {
                        foreach (var k in keyMatrix.Where(o => o.Key.Selected))
                            k.Key.Selected = false;
                        selection.Key.Selected = true;
                    }
                    else
                        selection.Key.Selected = !selection.Key.Selected;
                }
                Console.Clear();
            }
            return keyMatrix.Where(o => o.Key.Selected == true);
        }
        public IEnumerable<KeyValuePair<ConsoleKeySelection, T>> PromptKeyMatrix<T>(ICollection<T> collection, string selectionTitle, MatrixSelectionMethod method = MatrixSelectionMethod.SelectMany)
        {
            var keyMatrix = CreateKeyMatrix<T>(collection);
            return PromptKeyMatrix<T>(keyMatrix, collection, selectionTitle, method);
        }
        public T PromptKeyMatrix<T>(ICollection<T> collection, string selectionTitle)
        {
            return PromptKeyMatrix<T>(collection, selectionTitle, MatrixSelectionMethod.SelectOne).First(o => o.Key.Selected == true).Value;
        }
        public enum MatrixSelectionMethod { SelectOne, SelectMany };
        public void OutputObject(object o, int depth = 0)
        {
            if (o is System.Collections.ICollection)
            {
                if (((System.Collections.ICollection)o).Count > 0)
                {
                    var enumerator = ((System.Collections.ICollection)o).GetEnumerator();
                    enumerator.MoveNext();
                    Console.WriteLine(new string(' ', depth) + enumerator.Current.GetType().Name + "s[" + ((System.Collections.ICollection)o).Count + "]");
                    Console.WriteLine(new string(' ', depth) + "{");
                    foreach (var c in (System.Collections.ICollection)o)
                    {
                        OutputObject(c, depth + 2);
                    }
                    Console.WriteLine(new string(' ', depth) + "}");
                }
            }
            else
            {
                if (o.GetType().GetProperties().Count() == 0)
                {
                    Console.WriteLine(new string(' ', depth) + o);
                }
                else
                {
                    Console.WriteLine(new string(' ', depth) + "{");
                    foreach (var propertyInfo in o.GetType().GetProperties())
                    {
                        var targetProperty = o.GetType().GetProperty(propertyInfo.Name);
                        object outputValue = targetProperty.GetValue(o);
                        if (outputValue == null)
                            Console.WriteLine(new string(' ', depth + 1) + propertyInfo.Name + "=null");
                        else if (outputValue is System.Collections.IList || outputValue is System.Collections.IDictionary)
                        {
                            if (((System.Collections.ICollection)outputValue).Count == 0)
                                Console.WriteLine(new string(' ', depth + 1) + propertyInfo.Name + "[" + ((System.Collections.ICollection)outputValue).Count + "] { }");
                            else
                            {
                                Console.WriteLine(new string(' ', depth + 1) + propertyInfo.Name + "[" + ((System.Collections.ICollection)outputValue).Count + "]");
                                Console.WriteLine(new string(' ', depth + 1) + "{");
                                foreach (object child in (System.Collections.ICollection)outputValue)
                                {
                                    if (child.GetType().BaseType == typeof(System.Enum))
                                        Console.WriteLine(new string(' ', depth + 2) + child.GetType().Name + "=" + child);
                                    else
                                    {
                                        Console.WriteLine(new string(' ', depth + 2) + child.GetType().Name);
                                        Console.WriteLine(new string(' ', depth + 2) + "{");
                                        OutputObject(child, depth + 2);
                                        Console.WriteLine(new string(' ', depth + 2) + "}");
                                    }
                                }
                                Console.WriteLine(new string(' ', depth + 1) + "}");
                            }
                        }
                        else if (outputValue.GetType().Assembly == targetAssembly)
                        {
                            if (outputValue.GetType().BaseType == typeof(System.Enum))
                                Console.WriteLine(new string(' ', depth + 1) + outputValue.GetType().Name + "=" + outputValue);
                            else
                            {
                                Console.WriteLine(new string(' ', depth + 1) + outputValue.GetType().Name + " ");
                                Console.WriteLine(new string(' ', depth + 1) + "{");
                                OutputObject(outputValue, depth + 2);
                                Console.WriteLine(new string(' ', depth + 1) + "}");
                            }
                        }
                        else
                            Console.WriteLine(new string(' ', depth + 1) + propertyInfo.Name + "=" + targetProperty.GetValue(o));
                    }
                    Console.WriteLine(new string(' ', depth) + "}");
                }
            }
        }
        public static string ToDelimitedList(List<string> list, string delimiter)
        {
            string delimited = "";
            foreach (string s in list)
                delimited += string.Format("{0}{1}", delimiter, s);
            if (delimited.Contains(delimiter))
                return delimited.Substring(delimiter.Length);
            else
                return delimited;
        }
    }
}
