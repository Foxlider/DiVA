using System;
using System.IO;
using Discord;
using Discord.Commands;
using DiVA.Services;
using DiVA.Services.Youtube;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace DiVATests
{
    [TestFixture, Category("MainTests")]
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            new Logger();
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