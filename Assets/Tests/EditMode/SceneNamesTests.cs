using System.Collections.Generic;
using LastTrain.Core;
using NUnit.Framework;

namespace LastTrain.Tests.EditMode
{
    public class SceneNamesTests
    {
        [Test]
        public void AllSceneNames_AreNonEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(SceneNames.Bootstrap));
            Assert.IsFalse(string.IsNullOrEmpty(SceneNames.MainMenu));
            Assert.IsFalse(string.IsNullOrEmpty(SceneNames.Game));
            Assert.IsFalse(string.IsNullOrEmpty(SceneNames.Result));
        }

        [Test]
        public void AllSceneNames_AreUnique()
        {
            var names = new List<string>
            {
                SceneNames.Bootstrap,
                SceneNames.MainMenu,
                SceneNames.Game,
                SceneNames.Result
            };

            var unique = new HashSet<string>(names);
            Assert.AreEqual(names.Count, unique.Count, "Scene 이름 상수에 중복이 있습니다.");
        }
    }
}
