DELETE
  FROM [dbo].[Tickets]
 WHERE [dbo].[Tickets].[FlightId] IN (SELECT [dbo].[Flights].[Id]
		                                FROM [dbo].[Flights]
                                       WHERE [dbo].[Flights].[Destination] = 'Ayn Halagim');
DELETE
  FROM [dbo].[Flights]
 WHERE [dbo].[Flights].[Destination] = 'Ayn Halagim';