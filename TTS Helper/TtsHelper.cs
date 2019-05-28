using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;

namespace TTS_Helper
{
    public static class TtsHelper
    {
        private static SpeechSynthesizer _synth;
        private static List<VoiceInfo> _voices = new List<VoiceInfo>();

        private static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("No arguments sent. Please provide the arguments for the application to run\n"
                                + "- string     Path of the output .wav file\n"
                                + "- string     Language for the bot (en-US/fr-FR)\n"
                                + "- string[]   Text to say\n\nPress any key to exit...");
                Console.ReadKey();
                Environment.Exit(-1);
            }
            _synth = new SpeechSynthesizer();

            // Output information about all of the installed voices.
            _voices = _synth.GetInstalledVoices()
                            .Where(v =>
                                       (v.VoiceInfo.Id == "MSTTS_V110_enUS_EvaM")
                                    || (v.VoiceInfo.Id == "MSTTS_V110_frFR_NathalieM"))
                            .Select(v => v.VoiceInfo)
                            .ToList();
            if (_voices == null || _voices.Count < 1)
            {
                Console.WriteLine("Voices were not recognized. Please install the required TTS voices to proceed.\nPress any key to exit...");
                Console.ReadKey();
                Environment.Exit(-2);
            }
            var said = string.Join(" ", args.Skip(2));
            TTSToFile(said, Path.Combine(args[0]), args[1]);
        }

        private static void TTSToFile(string said, string path, string culture = "en-US")
        {
            if (_synth == null) { throw new InvalidOperationException("No speech synthetizer have been initiated."); }
            if (culture != "en-US" && culture != "fr-FR")
            { throw new InvalidOperationException("Only 'en-US' and 'fr-FR' cultures are supported right now."); }
            if (!Directory.Exists(Path.GetDirectoryName(path))) { Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Output path cannot be empty")); }
            _synth.SetOutputToWaveFile(path);
            PromptBuilder builder = new PromptBuilder(new System.Globalization.CultureInfo(culture));
            builder.StartStyle(new PromptStyle(PromptRate.Medium));
            builder.StartParagraph();
            foreach (var sentence in said.Split('\n'))
            {
                builder.StartSentence();
                builder.AppendText(sentence);
                builder.EndSentence();
            }
            builder.EndParagraph();
            builder.EndStyle();

            try
            { _synth.SelectVoice(GetVoice(culture)); }
            catch
            { throw new InvalidOperationException($"Could not select a voice with culture {culture}."); }

            _synth.Speak(builder);
        }

        private static string GetVoice(string culture)
        { return _voices.Where(v => v.Culture.ToString() == culture).Select(v => v.Name).FirstOrDefault() ?? throw new InvalidOperationException("No voices have been initiated."); }
    }
}
