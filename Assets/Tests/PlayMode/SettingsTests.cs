using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace FogboundMaze.Tests
{
    public sealed class SettingsTests
    {
        [UnityTest]
        public IEnumerator LanguageAndMutedVolumesSurviveSceneReload()
        {
            var chinese=PortfolioSettings.Chinese;var sound=PortfolioSettings.SoundEnabled;
            var master=PortfolioSettings.Master;var music=PortfolioSettings.Music;var effects=PortfolioSettings.Effects;
            try
            {
                SceneManager.LoadScene("Main");yield return null;yield return null;yield return null;
                PortfolioSettings.SetLanguage(true);
                var title=GameDirector.Instance.Hud.transform.Find("Title Screen/Game Title").GetComponent<Text>();
                title.GetComponent<LocalizedLabel>().Refresh();Assert.That(title.text,Is.EqualTo("迷宫\n逃生"));
                GameDirector.Instance.ShowGuide();
                GameDirector.Instance.Hud.transform.Find("Field Guide/Chapter 7").GetComponent<Button>().onClick.Invoke();
                foreach(var label in GameDirector.Instance.Hud.GetComponentsInChildren<LocalizedLabel>()) label.Refresh();
                var guideText=string.Join("\n",System.Array.ConvertAll(GameDirector.Instance.Hud.GetComponentsInChildren<Text>(),label=>label.text));
                Assert.That(guideText,Does.Contain("瘴气与避难灯"));
                Assert.That(guideText,Does.Not.Contain("Miasma drains"));
                PortfolioSettingsMenu.Instance.Open();Assert.That(PortfolioSettings.IsOpen,Is.True);
                PortfolioSettings.SetLanguage(false);title.GetComponent<LocalizedLabel>().Refresh();Assert.That(title.text,Is.EqualTo("MAZE\nESCAPE"));
                PortfolioSettings.SetVolume("Master",.41f);PortfolioSettings.SetVolume("Effects",.22f);PortfolioSettings.SetVolume("Music",.18f);PortfolioSettings.SetSound(false);
                PortfolioSettingsMenu.Instance.Close();Assert.That(AudioListener.volume,Is.Zero);
                SceneManager.LoadScene("Main");yield return null;yield return null;yield return null;
                Assert.That(PortfolioSettings.Chinese,Is.False);Assert.That(PortfolioSettings.Effects,Is.EqualTo(.22f));Assert.That(PortfolioSettings.Music,Is.EqualTo(.18f));Assert.That(AudioListener.volume,Is.Zero);
                PortfolioSettings.SetSound(true);Assert.That(AudioListener.volume,Is.EqualTo(.41f));
                Assert.That(GameDirector.Instance.Player.GetComponent<AudioSource>().volume,Is.EqualTo(.22f));
            }
            finally
            {
                PortfolioSettings.SetLanguage(chinese);PortfolioSettings.SetSound(sound);PortfolioSettings.SetVolume("Master",master);PortfolioSettings.SetVolume("Music",music);PortfolioSettings.SetVolume("Effects",effects);PortfolioSettings.Save();Time.timeScale=1;
            }
        }
    }
}
