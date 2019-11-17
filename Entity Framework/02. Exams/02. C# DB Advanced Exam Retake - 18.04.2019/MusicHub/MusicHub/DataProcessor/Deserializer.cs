﻿namespace MusicHub.DataProcessor
{
    using AutoMapper;
    using Data;
    using MusicHub.Data.Models;
    using MusicHub.Data.Models.Enums;
    using MusicHub.DataProcessor.ImportDtos;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data";

        private const string SuccessfullyImportedWriter
            = "Imported {0}";
        private const string SuccessfullyImportedProducerWithPhone
            = "Imported {0} with phone: {1} produces {2} albums";
        private const string SuccessfullyImportedProducerWithNoPhone
            = "Imported {0} with no phone number produces {1} albums";
        private const string SuccessfullyImportedSong
            = "Imported {0} ({1} genre) with duration {2}";
        private const string SuccessfullyImportedPerformer
            = "Imported {0} ({1} songs)";

        public static string ImportWriters(MusicHubDbContext context, string jsonString)
        {
            var writersDto = JsonConvert.DeserializeObject<ImportWriterDto[]>(jsonString);

            List<Writer> writers = new List<Writer>();

            StringBuilder sb = new StringBuilder();

            foreach (var writerDto in writersDto)
            {
                var writer = Mapper.Map<Writer>(writerDto);
                bool isValidWriter = IsValid(writer);

                if (isValidWriter == false)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                writers.Add(writer);

                sb.AppendLine(string.Format(SuccessfullyImportedWriter, writer.Name));
            }

            context.Writers.AddRange(writers);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportProducersAlbums(MusicHubDbContext context, string jsonString)
        {
            var producersDto = JsonConvert.DeserializeObject<ImportProducerDto[]>(jsonString);

            List<Producer> producers = new List<Producer>();

            StringBuilder sb = new StringBuilder();

            foreach (var producerDto in producersDto)
            {
                var producer = Mapper.Map<Producer>(producerDto);
                bool isValidProducer = IsValid(producer);
                bool isValidAlbums = producerDto.Albums.Any(a => IsValid(a) == false);

                if (isValidProducer == false || isValidAlbums == true)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                foreach (var albumDto in producerDto.Albums)
                {
                    var album = Mapper.Map<Album>(albumDto);
                    producer.Albums.Add(album);
                }

                producers.Add(producer);

                if (producer.PhoneNumber == null)
                {
                    sb.AppendLine(string.Format(SuccessfullyImportedProducerWithNoPhone,
                        producer.Name,
                        producer.Albums.Count()));
                }
                else
                {
                    sb.AppendLine(string.Format(SuccessfullyImportedProducerWithPhone,
                        producer.Name,
                        producer.PhoneNumber,
                        producer.Albums.Count()));
                }
            }

            context.Producers.AddRange(producers);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportSongs(MusicHubDbContext context, string xmlString)
        {
            var xmlSerializer = new XmlSerializer(typeof(ImportSongDto[]), new XmlRootAttribute("Songs"));
            var songsDto = (ImportSongDto[])xmlSerializer.Deserialize(new StringReader(xmlString));

            List<Song> songs = new List<Song>();

            StringBuilder sb = new StringBuilder();

            foreach (var songDto in songsDto)
            {
                bool isValidGenre = Enum.IsDefined(typeof(Genre), songDto.Genre);
                bool isValidAlbum = context.Albums.Any(a => a.Id == songDto.AlbumId);
                bool isValidWriter = context.Writers.Any(a => a.Id == songDto.WriterId);

                if (isValidGenre == false || isValidAlbum == false || isValidWriter == false)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var song = Mapper.Map<Song>(songDto);
                bool isValidSong = IsValid(song);

                if (isValidSong == false)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                songs.Add(song);

                sb.AppendLine(string.Format(SuccessfullyImportedSong,
                    song.Name,
                    song.Genre.ToString(),
                    song.Duration.ToString(@"hh\:mm\:ss")));
            }

            context.Songs.AddRange(songs);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportSongPerformers(MusicHubDbContext context, string xmlString)
        {
            var xmlSerializer = new XmlSerializer(typeof(ImportPerformerDto[]), new XmlRootAttribute("Performers"));
            var performersDto = (ImportPerformerDto[])xmlSerializer.Deserialize(new StringReader(xmlString));

            List<Performer> performers = new List<Performer>();

            StringBuilder sb = new StringBuilder();

            foreach (var performerDto in performersDto)
            {
                var performer = Mapper.Map<Performer>(performerDto);
                bool isValidPerformer = IsValid(performer);

                var songs = context.Songs.Select(x => x.Id).ToList();
                var songsExists = performerDto.PerformersSongs
                    .Select(x => x.Id)
                    .All(value => songs.Contains(value));

                if (isValidPerformer == false || songsExists == false)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                foreach (var songDto in performerDto.PerformersSongs)
                {
                    SongPerformer songPerformer = new SongPerformer
                    {
                        Performer = performer,
                        PerformerId = performer.Id,
                        Song = context.Songs.Find(songDto.Id),
                        SongId = songDto.Id
                    };

                    performer.PerformerSongs.Add(songPerformer);
                }

                performers.Add(performer);

                sb.AppendLine(string.Format(SuccessfullyImportedPerformer,
                    performer.FirstName,
                    performer.PerformerSongs.Count()));
            }

            context.Performers.AddRange(performers);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}