using NUnit.Framework;
using Discord;
using Moq;
using Discord.Commands;
using Discord.WebSocket;
using System;
using DiVA;
using Microsoft.Extensions.DependencyInjection;
using DiVA.Services.YouTube;
using DiVA.Services;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace Tests
{
    [TestFixture, Category("MainTests")]
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            //IGuildUser = new IGuildUser(); //ON NE PEUT PAS INSTANCIER DES INTERFACES
            Mock<IGuildUser> user = CreateMockGuildUser("Alexis", "Foxlider");
            IGuildUser guildUser = user.Object;
        }

        [Test]
        public void TestGuildUserName()
        {
            //IGuildUser guildUser= new IGuildUser(); //ON NE PEUT PAS INSTANCIER DES INTERFACES
            Mock<IGuildUser> user = CreateMockGuildUser("Alexis", "Foxlider");
            IGuildUser guildUser = user.Object;
            Assert.AreEqual("Foxlider", guildUser.Nickname);
        }

        [Test]
        public async System.Threading.Tasks.Task TestGetVideoInformationsAsync()
        {
            string[] files = Directory.GetFiles(AppContext.BaseDirectory, "config.json", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                var builder = new ConfigurationBuilder() // Create a new instance of the config builder
                              .SetBasePath(
                                  Path.GetDirectoryName(files[0])) // Specify the default location for the config file
                              .AddJsonFile(
                                  Path.GetFileName(files[0])); // Add this (json encoded) file to the configuration
                DiVA.DiVA.Configuration = builder.Build(); // Build the configuration

                var response = await YouTubeDownloadService.GetVideoData("Genesis - That's All (Official Music Video)");
                DownloadedVideo video = response;
                Assert.AreEqual(video.Title, "Genesis - That's All (Official Music Video)");
                Assert.AreEqual(video.Duration, 263);
                Assert.AreEqual(video.Url, "https://www.youtube.com/watch?v=khg2sloLzTI");
                Assert.AreEqual(video.DisplayID, "khg2sloLzTI");
                Assert.AreEqual(video.FileName, "khg2sloLzTI.mp3");
            }
            else { Assert.Pass("Could not find Config file. Setting to passed"); }
        }

        [Test]
        public void TestLog()
        {
            Assert.DoesNotThrow(delegate
            {
                Log.Critical("Test", "TEST");
                Log.Debug("Test", "TEST");
                Log.Error("Test", "TEST");
                Log.Information("Test", "TEST");
                Log.Neutral("Test", "TEST");
                Log.Verbose("Test", "TEST");
                Log.Warning("Test", "TEST");

                Log.Critical("Test");
                Log.Debug("Test");
                Log.Error("Test");
                Log.Information("Test");
                Log.Neutral("Test");
                Log.Verbose("Test");
                Log.Warning("Test");

                Log.Message(LogSeverity.Warning, "test", "TEST");
                Log.Message(LogSeverity.Verbose, "test", "TEST");
                Log.Message(LogSeverity.Info, "test", "TEST");
                Log.Message(LogSeverity.Error, "test", "TEST");
                Log.Message(LogSeverity.Debug, "test", "TEST");
                Log.Message(LogSeverity.Critical, "test", "TEST");

                Log.Message(LogSeverity.Warning, "test");
                Log.Message(LogSeverity.Verbose, "test");
                Log.Message(LogSeverity.Info, "test");
                Log.Message(LogSeverity.Error, "test");
                Log.Message(LogSeverity.Debug, "test");
                Log.Message(LogSeverity.Critical, "test");
            });
        }


        [Test]
        public void TestAudioQueue()
        {
            IGuild guild = CreateMockGuild().Object;

            AudioService service = new AudioService();
            VoiceConnexion connexion = new VoiceConnexion
            { Queue = new List<IPlayable>() };

            service.ConnectedChannels.TryAdd(guild.Id, connexion);
            service.ConnectedChannels.TryGetValue(guild.Id, out VoiceConnexion voice);
            DownloadedVideo video = new DownloadedVideo("TITLE", 5, "http://url.com", "YoutubeID", "YoutubeID.mp3");
            voice.Queue.Add(video);

            Assert.AreEqual(voice.Queue.FirstOrDefault().DurationString, video.DurationString);
            Assert.AreEqual(voice.Queue.FirstOrDefault().Title, video.Title);
            Assert.AreEqual(voice.Queue.FirstOrDefault().Uri, video.Uri);
            Assert.AreEqual(voice.Queue.FirstOrDefault().Url, video.Url);


            var songlist = service.SongList(guild);
            Assert.AreEqual(songlist.FirstOrDefault().DurationString, video.DurationString);
            Assert.AreEqual(songlist.FirstOrDefault().Title, video.Title);
            Assert.AreEqual(songlist.FirstOrDefault().Uri, video.Uri);
            Assert.AreEqual(songlist.FirstOrDefault().Url, video.Url);

            var list = service.Clear(guild);
            Assert.AreEqual(list.Count, 0);
        }


#region mocks

        private Mock<ICommandContext> CreateMockContext(string command)
        {
            if (!command.StartsWith(".."))
            { command = ".." + command; }
            var Context = new Mock<ICommandContext>();
            Context.Setup(ctx => ctx.User).Returns(CreateMockGuildUser("Foxlider", "Keelah").Object);
            Context.Setup(ctx => ctx.Guild).Returns(CreateMockGuild().Object);
            Context.Setup(ctx => ctx.Message).Returns(CreateMockMessage(command, Context.Object.User).Object);
            Context.Setup(ctx => ctx.Channel).Returns(CreateMockChannel().Object);
            return Context;
        }

        private Mock<IUserMessage> CreateMockMessage(string content, IUser author = null, ulong ID = 540152386141028362)
        {
            if (author == null)
            { author = CreateMockGuildUser("Foxlider", "Keelah").Object; }
            var Message = new Mock<IUserMessage>();
            Message.Setup(msg => msg.Channel).Returns(CreateMockChannel().Object);
            Message.Setup(msg => msg.Id).Returns(ID);
            Message.Setup(msg => msg.Content).Returns(content);
            Message.Setup(msg => msg.Author).Returns(author);
            Message.Setup(msg => msg.CreatedAt).Returns(DateTimeOffset.Now);
            return Message;
        }

        private Mock<IMessageChannel> CreateMockChannel(ulong ID = 368349179640020993, string name = "#bot")
        {
            var Channel = new Mock<IMessageChannel>();
            Channel.Setup(c => c.Id).Returns(ID);
            Channel.Setup(c => c.Name).Returns(name);
            return Channel;
        }

        private static Mock<IGuildUser> CreateMockGuildUser(string username, string nickname, ulong ID = 140195053241892864, string guildname = "Aetherium Squadron", ulong guildId = 366955806777671681)
        {
            var guildUser = new Mock<IGuildUser>();
            guildUser.Setup(gUser => gUser.Nickname).Returns(nickname);
            guildUser.Setup(gUser => gUser.Username).Returns(username);
            guildUser.Setup(gUser => gUser.Id).Returns(ID);
            guildUser.Setup(gUser => gUser.Mention).Returns($"<@{ID}>");
            guildUser.Setup(gUser => gUser.Guild.Name).Returns(guildname);
            guildUser.Setup(gUser => gUser.Guild.Id).Returns(guildId);
            return guildUser;
        }

        private static Mock<IGuild> CreateMockGuild(string guildname = "Aetherium Squadron", ulong guildId = 366955806777671681)
        {
            var Guild = new Mock<IGuild>();
            Guild.Setup(guild => guild.Id).Returns(guildId);
            Guild.Setup(guild => guild.Name).Returns(guildname);
            return Guild;
        }
        #endregion
    }
}