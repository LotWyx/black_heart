using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BlackHeart.Tests
{
    // Порядок в репозитории: у каждого ассета есть .meta и наоборот. Забытый .meta — частая причина
    // «Missing»-ссылок у других участников после git pull.
    public class ProjectIntegrityTests
    {
        static string AssetsDir => Application.dataPath;

        [Test]
        public void EveryAssetHasMeta()
        {
            var missing = Directory.EnumerateFileSystemEntries(AssetsDir, "*", SearchOption.AllDirectories)
                .Where(p => !p.EndsWith(".meta") && !Path.GetFileName(p).StartsWith("."))
                .Where(p => !File.Exists(p + ".meta"))
                .Select(p => p.Substring(AssetsDir.Length))
                .ToList();
            Assert.IsEmpty(missing, "Нет .meta у:\n" + string.Join("\n", missing));
        }

        [Test]
        public void NoOrphanMeta()
        {
            var orphans = Directory.EnumerateFiles(AssetsDir, "*.meta", SearchOption.AllDirectories)
                .Where(m => { var a = m.Substring(0, m.Length - 5); return !File.Exists(a) && !Directory.Exists(a); })
                .Select(p => p.Substring(AssetsDir.Length))
                .ToList();
            Assert.IsEmpty(orphans, ".meta без ассета:\n" + string.Join("\n", orphans));
        }
    }
}
