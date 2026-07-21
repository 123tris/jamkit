using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Metz.JamKit.Tests.Editor
{
    /// <summary>
    /// Characterization tests for the JSON save service. Each fixture writes under a unique
    /// folder in persistentDataPath and deletes it afterwards.
    /// </summary>
    public class SaveServiceSOTests
    {
        SaveServiceSO _save;
        string _root;

        [Serializable]
        class SaveBlob
        {
            public int Level;
            public string Name;
        }

        [SetUp]
        public void SetUp()
        {
            _save = ScriptableObject.CreateInstance<SaveServiceSO>();
            _save.Folder = "saves-test-" + Guid.NewGuid().ToString("N");
            _root = Path.Combine(Application.persistentDataPath, _save.Folder);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_save);
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void WriteReadRoundtripsAClass()
        {
            _save.Write("slot1", new SaveBlob { Level = 7, Name = "jam" });
            var loaded = _save.Read<SaveBlob>("slot1");
            Assert.AreEqual(7, loaded.Level);
            Assert.AreEqual("jam", loaded.Name);
        }

        [Test]
        public void WriteReadRoundtripsAStruct()
        {
            _save.Write("pos", new Vector3(1.5f, -2f, 3f));
            Assert.AreEqual(new Vector3(1.5f, -2f, 3f), _save.Read<Vector3>("pos"));
        }

        [Test]
        public void WriteReadRoundtripsAList()
        {
            _save.Write("scores", new List<int> { 3, 1, 4 });
            var loaded = _save.Read<List<int>>("scores");
            CollectionAssert.AreEqual(new[] { 3, 1, 4 }, loaded);
        }

        [Test]
        public void ReadMissingKeyReturnsFallback()
        {
            Assert.AreEqual(42, _save.Read("nothing-here", 42));
            Assert.IsNull(_save.Read<SaveBlob>("nothing-here"));
        }

        [Test]
        public void ReadCorruptJsonLogsErrorAndReturnsFallback()
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(Path.Combine(_root, "bad.json"), "{ this is not json");

            LogAssert.Expect(LogType.Error, new Regex(@"\[JamKit\] Save\.Read 'bad' failed"));
            Assert.AreEqual(42, _save.Read("bad", 42));
        }

        [Test]
        public void HasDeleteAndDeleteAllManageFiles()
        {
            _save.Write("a", 1);
            _save.Write("b", 2);
            Assert.IsTrue(_save.Has("a"));

            _save.Delete("a");
            Assert.IsFalse(_save.Has("a"));
            Assert.IsTrue(_save.Has("b"));

            _save.DeleteAll();
            Assert.IsFalse(_save.Has("b"));
        }
    }
}
