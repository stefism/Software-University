﻿namespace MusicHub.DataProcessor
{
    using Data;
    using MusicHub.DataProcessor.ExportDtos;
    using Newtonsoft.Json;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using Formatting = Newtonsoft.Json.Formatting;

    public class Serializer
    {
        public static string ExportAlbumsInfo(MusicHubDbContext context, int producerId)
        {
            var albums = context.Albums
                .Where(a => a.ProducerId == producerId)
                .OrderByDescending(a => a.Price)
                .Select(a => new ExportAlbumDto
                {
                    AlbumName = a.Name,
                    ProducerName = a.Producer.Name,
                    ReleaseDate = a.ReleaseDate.ToString(@"MM/dd/yyyy"),
                    Songs = a.Songs.Select(s => new ExportSongsDto
                    {
                        SongName = s.Name,
                        Writer = s.Writer.Name,
                        Price = s.Price.ToString("F2")
                    })
                    .OrderByDescending(s => s.SongName)
                    .ThenBy(s => s.Writer)
                    .ToList(),
                    AlbumPrice = a.Price.ToString("F2")
                })
                .ToList();

            var json = JsonConvert.SerializeObject(albums, Formatting.Indented);

            return json;
        }

        public static string ExportSongsAboveDuration(MusicHubDbContext context, int duration)
        {
            var songs = context.Songs
                .Where(s => s.Duration.TotalSeconds > duration)
                .OrderBy(s => s.Name)
                .ThenBy(s => s.Writer.Name)
                .ThenBy(s => s.SongPerformers
                .Select(sp => $"{sp.Performer.FirstName} {sp.Performer.LastName}")
                .FirstOrDefault())
                .Select(s => new ExportSongInRangeDto
                {
                    SongName = s.Name,
                    Writer = s.Writer.Name,
                    Performer = s.SongPerformers
                    .Select(sp => $"{sp.Performer.FirstName} {sp.Performer.LastName}")
                    .FirstOrDefault(),
                    AlbumProducer = s.Album.Producer.Name,
                    Duration = s.Duration.ToString(@"hh\:mm\:ss")
                })
                .ToArray();

            var xmlSerializer = new XmlSerializer(typeof(ExportSongInRangeDto[]), new XmlRootAttribute("Songs"));

            var sb = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            xmlSerializer.Serialize(new StringWriter(sb), songs, namespaces);

            return sb.ToString().TrimEnd();
        }
    }
}