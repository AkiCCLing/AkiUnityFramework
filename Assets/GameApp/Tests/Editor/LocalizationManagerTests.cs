using AkiFramework;
using NUnit.Framework;
using UnityEngine;

namespace GameApp.Tests.Editor
{
    public class LocalizationManagerTests
    {
        private const string LanguageKey = "GameApp.Language";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(LanguageKey);
            LocalizationManager.SetLanguage(LocalizationManager.ZhCn, save: false);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(LanguageKey);
        }

        [Test]
        public void SetLanguage_DispatchesNormalizedSignal()
        {
            string receivedLanguage = null;

            void Handler(string language) => receivedLanguage = language;

            Signals.Get<ChangeLanguageSignal>().AddListener(Handler);
            LocalizationManager.SetLanguage("EN");
            Signals.Get<ChangeLanguageSignal>().RemoveListener(Handler);

            Assert.That(LocalizationManager.CurrentLanguage, Is.EqualTo(LocalizationManager.En));
            Assert.That(receivedLanguage, Is.EqualTo(LocalizationManager.En));
        }

        [Test]
        public void LoadSavedLanguage_RestoresPersistedValue()
        {
            LocalizationManager.SetLanguage(LocalizationManager.En);
            LocalizationManager.SetLanguage(LocalizationManager.ZhCn, save: false);

            LocalizationManager.LoadSavedLanguage();

            Assert.That(LocalizationManager.CurrentLanguage, Is.EqualTo(LocalizationManager.En));
        }
    }
}
