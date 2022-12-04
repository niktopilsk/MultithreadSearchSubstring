using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadSearchSubstring
{
    internal static class Program
    {
        private const int ThreadsCount = 12;
        private static readonly List<Task> Tasks = new();
        private static readonly ConcurrentDictionary<int, (string, List<int>)> Result = new();

        private static async Task Main(string[] args)
        {
            Console.WriteLine("Path for file: ");
            var path = Console.ReadLine();
            Console.WriteLine("Substring for search: ");
            var substring = Console.ReadLine();
            RunSearch(path, substring);
            Task.WaitAll(Tasks.ToArray());
            var resultFilePath = Path.Combine(Path.Combine(Path.GetDirectoryName(path)!),
                $"Result of search \'{substring}\' in {Path.GetFileNameWithoutExtension(path)}.txt");
            await File.WriteAllTextAsync(resultFilePath, "");
            foreach (var (key, value) in Result)
                await File.AppendAllTextAsync(resultFilePath,
                    $"Line Number:{key}. Indexes: {string.Join(",", value.Item2)}. {value.Item1}\n");
        }

        private static async void RunSearch(string path, string substring)
        {
            var index = 1;
            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            switch (lines.Length)
            {
                case < 1:
                    return;
                case 1:
                    var t = new Thread(() =>
                    {
                        Seach(lines, substring, 0);
                    });
                    t.Start();
                    var resultFilePath = Path.Combine(Path.Combine(Path.GetDirectoryName(path)!),
                        $"Result of search \'{substring}\' in {Path.GetFileNameWithoutExtension(path)}.txt");
                    await File.WriteAllTextAsync(resultFilePath, "");
                    foreach (var (key, value) in Result)
                        await File.AppendAllTextAsync(resultFilePath,
                            $"Line Number:{key}. Indexes: {string.Join(",", value.Item2)}. {value.Item1}\n");
                    break;
                default:
                {
                    var linesCount = lines.Length / ThreadsCount;
                    for (var i = 0; i < ThreadsCount; i++)
                    {
                        var part = lines.Skip(linesCount * i + 1).Take(linesCount);
                        var index1 = index;
                        var task = Task.Run(async () =>
                        {
                            await Seach(part, substring, index1);
                        });
                        index = linesCount * i + 1;
                        Tasks.Add(task);
                    }

                    break;
                }
            }
        }

        private static Task Seach(
            IEnumerable<string> lines,
            string pattern,
            int stringNumber)
        {
            try
            {
                var indexes = new List<int>();
                foreach (var line in lines)
                {
                    indexes.AddRange(KmpSearch(pattern, line));
                    if (indexes.Count > 0)
                    {
                        Result.TryAdd(stringNumber, (line, indexes));
                        Console.WriteLine(
                            $"Line Number:{stringNumber}. Indexes: {string.Join(",", indexes)}. {line}\n");
                    }

                    indexes = new List<int>();
                    stringNumber++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Task.CompletedTask;
        }

        private static IEnumerable<int> KmpSearch(string pat, string txt)
        {
            var m = pat.Length;
            var n = txt.Length;
            var lps = new int[m];
            var j = 0;
            ComputeLpsArray(pat, m, lps);

            var i = 0;
            while (n - i >= m - j)
            {
                if (pat[j] == txt[i])
                {
                    j++;
                    i++;
                }

                if (j == m)
                {
                    yield return i - j;
                    j = lps[j - 1];
                }

                else if (i < n &&
                         pat[j] != txt[i])
                {
                    if (j != 0)
                    {
                        j = lps[j - 1];
                    }
                    else
                    {
                        i = i + 1;
                    }
                }
            }
        }

        private static void ComputeLpsArray(string pat, int m, IList<int> lps)
        {
            var len = 0;
            var i = 1;
            lps[0] = 0;

            while (i < m)
            {
                if (pat[i] == pat[len])
                {
                    len++;
                    lps[i] = len;
                    i++;
                }
                else
                {
                    if (len != 0)
                    {
                        len = lps[len - 1];
                    }
                    else
                    {
                        lps[i] = len;
                        i++;
                    }
                }
            }
        }
    }
}