﻿using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VimeoDotNet.Models;
using VimeoDotNet.Net;
using VimeoDotNet.Tests.Settings;
using VimeoDotNet.Parameters;

namespace VimeoDotNet.Tests
{
    [TestClass]
    //[Ignore] // Comment this line to run integration tests.
    public class VimeoClient_IntegrationTests
    {
        private VimeoApiTestSettings vimeoSettings;

		private const string TESTFILEPATH = @"Resources\test.mp4";
		// http://download.wavetlan.com/SVV/Media/HTTP/http-mp4.htm

		private const string TESTTEXTTRACKFILEPATH = @"Resources\test.vtt";

		[TestInitialize]
        public void SetupTest()
        {
            // Load the settings from a file that is not under version control for security
            // The settings loader will create this file in the bin/ folder if it doesn't exist
            vimeoSettings = Settings.SettingsLoader.LoadSettings(); 
        }
        
        [TestMethod]
        public void Integration_VimeoClient_GetReplaceVideoUploadTicket_CanGenerateStreamingTicket()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            UploadTicket ticket = client.GetReplaceVideoUploadTicket(vimeoSettings.VideoId);

            // assert
            Assert.IsNotNull(ticket);
        }

        [TestMethod]
        public void Integration_VimeoClient_GetUploadTicket_CanGenerateStreamingTicket()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            UploadTicket ticket = client.GetUploadTicket();

            // assert
            Assert.IsNotNull(ticket);
        }

        [TestMethod]
        public void Integration_VimeoClient_UploadEntireFile_UploadsFile()
        {
            // arrange
            long length;
            IUploadRequest completedRequest;
            using (var file = new BinaryContent(GetFullPath(TESTFILEPATH)))
            {
                length = file.Data.Length;
                VimeoClient client = CreateAuthenticatedClient();

                // act
                completedRequest = client.UploadEntireFile(file);
            }

            // assert
            Assert.IsNotNull(completedRequest);
            Assert.IsTrue(completedRequest.AllBytesWritten);
            Assert.IsTrue(completedRequest.IsVerifiedComplete);
            Assert.AreEqual(length, completedRequest.BytesWritten);
            Assert.IsNotNull(completedRequest.ClipUri);
            Assert.IsTrue(completedRequest.ClipId > 0);
        }

        [TestMethod]
        public void Integration_VimeoClient_DeleteVideo_DeletesVideo()
        {
            // arrange
            long length;
            IUploadRequest completedRequest;
            using (var file = new BinaryContent(GetFullPath(TESTFILEPATH)))
            {
                length = file.Data.Length;
                VimeoClient client = CreateAuthenticatedClient();
                // act
                completedRequest = client.UploadEntireFile(file);
                Assert.IsTrue(completedRequest.AllBytesWritten);
                Assert.IsNotNull(completedRequest);
                Assert.IsTrue(completedRequest.IsVerifiedComplete);
                Assert.AreEqual(length, completedRequest.BytesWritten);
                Assert.IsNotNull(completedRequest.ClipUri);
                Assert.IsTrue(completedRequest.ClipId.HasValue);
                client.DeleteVideo(completedRequest.ClipId.Value);
                Assert.IsNull(client.GetVideo(completedRequest.ClipId.Value));
            }
            // assert            
        }

        [TestMethod]
        public void Integration_VimeoClient_GetAccountInformation_RetrievesCurrentAccountInfo()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            User account = client.GetAccountInformation();

            // assert
            Assert.IsNotNull(account);
        }

		[TestMethod]
		public void Integration_VimeoClient_UpdateAccountInformation_UpdatesCurrentAccountInfo()
		{
			// first, ensure we can retrieve the current user...
			VimeoClient client = CreateAuthenticatedClient();
			User original = client.GetAccountInformation();
			Assert.IsNotNull(original);

			// next, update the user record with some new values...
			string testName = "King Henry VIII";
			string testBio = "";
			string testLocation = "England";

			User updated = client.UpdateAccountInformation(new EditUserParameters
			{
				Name = testName,
				Bio = testBio,
				Location = testLocation
			});

			// inspect the result and ensure the values match what we expect...
			// the vimeo api will set string fields to null if the value passed in is an empty string
			// so check against null if that is what we are passing in, otherwise, expect the passed value...
			if (string.IsNullOrEmpty(testName))
				Assert.IsNull(updated.name);
			else
				Assert.AreEqual(testName, updated.name);

			if (string.IsNullOrEmpty(testBio))
				Assert.IsNull(updated.bio);
			else
				Assert.AreEqual(testBio, updated.bio);

			if (string.IsNullOrEmpty(testLocation))
				Assert.IsNull(updated.location);
			else
				Assert.AreEqual(testLocation, updated.location);

			// restore the original values...
			User final = client.UpdateAccountInformation(new Parameters.EditUserParameters
			{
				Name = original.name ?? string.Empty,
				Bio = original.bio ?? string.Empty,
				Location = original.location ?? string.Empty
			});

			// inspect the result and ensure the values match our originals...
			if (string.IsNullOrEmpty(original.name))
			{
				Assert.IsNull(final.name);
			}
			else
			{
				Assert.AreEqual(original.name, final.name);
			}
				
			if (string.IsNullOrEmpty(original.bio))
			{
				Assert.IsNull(final.bio);
			}
			else
			{
				Assert.AreEqual(original.bio, final.bio);
			}
				
			if (string.IsNullOrEmpty(original.location))
			{
				Assert.IsNull(final.location);
			} 
			else
			{
				Assert.AreEqual(original.location, final.location);
			}			
		}


        [TestMethod]
        public void Integration_VimeoClient_GetUserInformation_RetrievesUserInfo()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            User user = client.GetUserInformation(vimeoSettings.UserId);

            // assert
            Assert.IsNotNull(user);
            Assert.AreEqual(vimeoSettings.UserId, user.id.Value);
        }

        [TestMethod]
        public void Integration_VimeoClient_GetAccountVideos_RetrievesCurrentAccountVideos()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            Paginated<Video> videos = client.GetUserVideos(vimeoSettings.UserId); 

            // assert
            Assert.IsNotNull(videos);
        }

        [TestMethod]
        public async Task Integration_VimeoClient_GetAccountVideos_SecondPage()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            Paginated<Video> videos = await client.GetVideosAsync(page: 2, perPage: 5);

            // assert
            Assert.IsNotNull(videos);
        }

        [TestMethod]
        public void Integration_VimeoClient_GetAccountVideo_RetrievesVideo()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            Video video = client.GetVideo(vimeoSettings.VideoId);

            // assert
            Assert.IsNotNull(video);
            Assert.IsTrue(video.pictures.Any(a => a.uri != null));
        }

        [TestMethod]
        public void Integration_VimeoClient_GetAccountAlbumVideos_RetrievesCurrentAccountAlbumVideos()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            Paginated<Video> videos = client.GetAlbumVideos(vimeoSettings.AlbumId, 1, null);

            // assert
            Assert.IsNotNull(videos);
            Assert.AreNotEqual(videos.data.Count, 0);
        }

        [TestMethod]
        public void Integration_VimeoClient_GetAccountAlbumVideo_RetrievesVideo()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            Video video = client.GetAlbumVideo(vimeoSettings.AlbumId, vimeoSettings.VideoId);

            // assert
            Assert.IsNotNull(video);
        }

        [TestMethod]
        public void Integration_VimeoClient_GetUserAlbumVideos_RetrievesUserAlbumVideos()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            Paginated<Video> videos = client.GetUserAlbumVideos(vimeoSettings.UserId, vimeoSettings.AlbumId);

            // assert
            Assert.IsNotNull(videos);
            Assert.AreNotEqual(videos.data.Count, 0);
        }

        [TestMethod]
        public void Integration_VimeoClient_GetUserAlbumVideo_RetrievesVideo()
        {
            // arrange
            VimeoClient client = CreateAuthenticatedClient();

            // act
            Video video = client.GetUserAlbumVideo(vimeoSettings.UserId, vimeoSettings.AlbumId, vimeoSettings.VideoId);

            // assert
            Assert.IsNotNull(video);
        }

		[TestMethod]
		public void Integration_VimeoClient_GetAccountAlbums_NotNull()
		{
			// arrange
			VimeoClient client = CreateAuthenticatedClient();

			// act
			Paginated<Album> albums = client.GetAlbums();

			// assert
			Assert.IsNotNull(albums);
		}

		[TestMethod]
		public void Integration_VimeoClient_AlbumVideoManagement()
		{
			VimeoClient client = CreateAuthenticatedClient();

			// assume this album and video are configured in the current account...
			long albumId = vimeoSettings.AlbumId;
			long videoId = vimeoSettings.VideoId;

			// add it...
			bool isAdded = client.AddToAlbum(albumId, videoId);
			Video addedVideo = client.GetAlbumVideo(albumId, videoId);
			bool isPresent = addedVideo != null;

			Assert.IsTrue(isAdded, "AddToAlbum failed.");
			Assert.IsTrue(isAdded == isPresent, "Returned value does not match actual presence of video.");
			
			// then remove it...
			bool isRemoved = client.RemoveFromAlbum(albumId, videoId);
			Video removedVideo = client.GetAlbumVideo(albumId, videoId);
			bool isAbsent = removedVideo == null;

			Assert.IsTrue(isRemoved, "RemoveFromAlbum failed.");
			Assert.IsTrue(isRemoved == isAbsent, "Returned value does not match actual abscence of video.");
		}

		[TestMethod]
		public void Integration_VimeoClient_AlbumManagement()
		{
			VimeoClient client = CreateAuthenticatedClient();
		
			// create a new album...
			string originalName = "Unit Test Album";
			string originalDesc = "This album was created via an automated test, and should be deleted momentarily...";

			Album newAlbum = client.CreateAlbum(new EditAlbumParameters()
			{
				Name = originalName,
				Description = originalDesc,
				Sort = EditAlbumSortOption.Newest,
				Privacy = EditAlbumPrivacyOption.Password,
				Password = "test"
			});

			Assert.IsNotNull(newAlbum);
			Assert.AreEqual(originalName, newAlbum.name);
			Assert.AreEqual(originalDesc, newAlbum.description);

			// retrieve albums for the current user...there should be at least one now...
			Paginated<Album> albums = client.GetAlbums();

			Assert.IsTrue(albums.total > 0);

			// update the album...
			string updatedName = "Unit Test Album (Updated)";
			Album updatedAlbum = client.UpdateAlbum(newAlbum.GetAlbumId().Value, new EditAlbumParameters()
			{
				Name = updatedName,
				Privacy = EditAlbumPrivacyOption.Anybody
			});

			Assert.AreEqual(updatedName, updatedAlbum.name);

			// delete the album...
			bool isDeleted = client.DeleteAlbum(updatedAlbum.GetAlbumId().Value);

			Assert.IsTrue(isDeleted);
		}

		[TestMethod]
		public void Integration_VimeoClient_GetUserAlbums_NotNull()
		{
			// arrange
			VimeoClient client = CreateAuthenticatedClient();

			// act
			Paginated<Album> albums = client.GetAlbums(vimeoSettings.UserId);

			// assert
			Assert.IsNotNull(albums);
		}

		[TestMethod]
		public async Task Integration_VimeoClient_GetTextTracksAsync()
		{
			// arrange
			VimeoClient client = CreateAuthenticatedClient();

			// act
			var texttracks = await client.GetTextTracksAsync(vimeoSettings.VideoId);
			
			// assert
			Assert.IsNotNull(texttracks);
		}

		[TestMethod]
		public async Task Integration_VimeoClient_GetTextTrackAsync()
		{
			// arrange
			VimeoClient client = CreateAuthenticatedClient();

			// act
			var texttrack = await client.GetTextTrackAsync(vimeoSettings.VideoId, vimeoSettings.TextTrackId);

			// assert
			Assert.IsNotNull(texttrack);
		}

		[TestMethod]
		public async Task Integration_VimeoClient_UpdateTextTrackAsync()
		{
			// arrange
			VimeoClient client = CreateAuthenticatedClient();
			var original = await client.GetTextTrackAsync(vimeoSettings.VideoId, vimeoSettings.TextTrackId);

			Assert.IsNotNull(original);

			// act
			// update the text track record with some new values...
			var testName = "NewTrackName";
			var testType = "subtitles";
			var testLanguage = "fr";
			var testActive = false;

			var updated = await client.UpdateTextTrackAsync(
									vimeoSettings.VideoId,
									vimeoSettings.TextTrackId,
									new TextTrack
									{
										name = testName,
										type = testType,
										language = testLanguage,
										active = testActive
									});

			// inspect the result and ensure the values match what we expect...
			// assert
			Assert.AreEqual(testName, updated.name);
			Assert.AreEqual(testType, updated.type);
			Assert.AreEqual(testLanguage, updated.language);
			Assert.AreEqual(testActive, updated.active);

			// restore the original values...
			var final = await client.UpdateTextTrackAsync(
									vimeoSettings.VideoId,
									vimeoSettings.TextTrackId,
									new TextTrack
									{
										name = original.name,
										type = original.type,
										language = original.language,
										active = original.active
									});

			// inspect the result and ensure the values match our originals...
			if (string.IsNullOrEmpty(original.name))
			{
				Assert.IsNull(final.name);
			}
			else
			{
				Assert.AreEqual(original.name, final.name);
			}

			if (string.IsNullOrEmpty(original.type))
			{
				Assert.IsNull(final.type);
			}
			else
			{
				Assert.AreEqual(original.type, final.type);
			}

			if (string.IsNullOrEmpty(original.language))
			{
				Assert.IsNull(final.language);
			}
			else
			{
				Assert.AreEqual(original.language, final.language);
			}

			Assert.AreEqual(original.active, final.active);
		}

		[TestMethod]
		public async Task Integration_VimeoClient_UploadTextTrackFileAsync()
		{
			// arrange
			VimeoClient client = CreateAuthenticatedClient();
			TextTrack completedRequest;
			using (var file = new BinaryContent(GetFullPath(TESTTEXTTRACKFILEPATH)))
			{
				// act
				completedRequest = await client.UploadTextTrackFileAsync(
								file,
								vimeoSettings.VideoId,
								new TextTrack
								{
									active = false,
									name = "UploadTest",
									language = "en",
									type = "captions"
								});
			}

			// assert
			Assert.IsNotNull(completedRequest);
			Assert.IsNotNull(completedRequest.uri);
		}

		[TestMethod]
		public async Task Integration_VimeoClient_DeleteTextTrack()
		{
			// arrange
			TextTrack completedRequest;
			VimeoClient client = CreateAuthenticatedClient();
			using (var file = new BinaryContent(GetFullPath(TESTTEXTTRACKFILEPATH)))
			{
				completedRequest = await client.UploadTextTrackFileAsync(
								file,
								vimeoSettings.VideoId,
								new TextTrack
								{
									active = false,
									name = "DeleteTest",
									language = "en",
									type = "captions"
								});
			}
			Assert.IsNotNull(completedRequest);
			Assert.IsNotNull(completedRequest.uri);
			var uri = completedRequest.uri;
			var trackId = System.Convert.ToInt64(uri.Substring(uri.LastIndexOf('/') + 1));
			// act
			await client.DeleteTextTrackAsync(vimeoSettings.VideoId, trackId);

			//assert
			var texttrack = await client.GetTextTrackAsync(vimeoSettings.VideoId, trackId);
			Assert.IsNull(texttrack);
		}

		private VimeoClient CreateUnauthenticatedClient()
        {
            return new VimeoClient(vimeoSettings.ClientId, vimeoSettings.ClientSecret);
        }

        private VimeoClient CreateAuthenticatedClient()
        {
            return new VimeoClient(vimeoSettings.AccessToken);
        }

        private string GetFullPath(string relativePath)
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)); // /bin/debug
            return Path.Combine(dir.Parent.Parent.FullName, relativePath);
        }
    }
}