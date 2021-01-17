// Copyright (c) Valdis Iljuconoks. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbLocalizationProvider.Abstractions;
using DbLocalizationProvider.Internal;
using Microsoft.Azure.Cosmos.Table;

namespace DbLocalizationProvider.Storage.AzureTables
{
    /// <summary>
    /// Repository for working with underlying Azure Tables storage.
    /// </summary>
    public class ResourceRepository : IResourceRepository
    {
        private readonly bool _enableInvariantCultureFallback;

        /// <summary>
        /// Creates new instance of the class.
        /// </summary>
        /// <param name="configurationContext">Configuration settings.</param>
        public ResourceRepository(ConfigurationContext configurationContext)
        {
            _enableInvariantCultureFallback = configurationContext.EnableInvariantCultureFallback;
        }

        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns>List of resources</returns>
        public IEnumerable<LocalizationResource> GetAll()
        {
            var partitionCondition = TableQuery.GenerateFilterCondition(nameof(LocalizationResourceEntity.PartitionKey),
                                                                        QueryComparisons.Equal,
                                                                        LocalizationResourceEntity.PartitionKey);

            var query = new TableQuery<LocalizationResourceEntity>().Where(partitionCondition);
            var table = GetTable();
            var result = table.ExecuteQuery(query);

            return result.Select(FromEntity).ToList();


            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand(@"
            //        SELECT
            //            r.Id,
            //            r.ResourceKey,
            //            r.Author,
            //            r.FromCode,
            //            r.IsHidden,
            //            r.IsModified,
            //            r.ModificationDate,
            //            r.Notes,
            //            t.Id as TranslationId,
            //            t.Value as Translation,
            //            t.Language,
            //            t.ModificationDate as TranslationModificationDate
            //        FROM [dbo].[LocalizationResources] r
            //        LEFT JOIN [dbo].[LocalizationResourceTranslations] t ON r.Id = t.ResourceId",
            //                             conn);

            //    var reader = cmd.ExecuteReader();
            //    var lookup = new Dictionary<string, LocalizationResource>();

            //    void CreateTranslation(SqlDataReader sqlDataReader, LocalizationResource localizationResource)
            //    {
            //        if (!sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("TranslationId")))
            //        {
            //            localizationResource.Translations.Add(new LocalizationResourceTranslation
            //            {
            //                Id =
            //                    sqlDataReader.GetInt32(
            //                        sqlDataReader.GetOrdinal("TranslationId")),
            //                ResourceId = localizationResource.Id,
            //                Value = sqlDataReader.GetStringSafe("Translation"),
            //                Language =
            //                    sqlDataReader.GetStringSafe("Language") ?? string.Empty,
            //                ModificationDate =
            //                    reader.GetDateTime(
            //                        reader.GetOrdinal("TranslationModificationDate")),
            //                LocalizationResource = localizationResource
            //            });
            //        }
            //    }

            //    while (reader.Read())
            //    {
            //        var key = reader.GetString(reader.GetOrdinal(nameof(LocalizationResource.ResourceKey)));
            //        if (lookup.TryGetValue(key, out var resource))
            //        {
            //            CreateTranslation(reader, resource);
            //        }
            //        else
            //        {
            //            var result = CreateResourceFromSqlReader(key, reader);
            //            CreateTranslation(reader, result);
            //            lookup.Add(key, result);
            //        }
            //    }

            //    return lookup.Values;
            //}
        }

        /// <summary>
        /// Gets resource by the key.
        /// </summary>
        /// <param name="resourceKey">The resource key.</param>
        /// <returns>Localized resource if found by given key</returns>
        /// <exception cref="ArgumentNullException">resourceKey</exception>
        public LocalizationResource GetByKey(string resourceKey)
        {
            if (resourceKey == null)
            {
                throw new ArgumentNullException(nameof(resourceKey));
            }

            var partitionCondition = TableQuery.GenerateFilterCondition(nameof(LocalizationResourceEntity.PartitionKey),
                                                                        QueryComparisons.Equal,
                                                                        LocalizationResourceEntity.PartitionKey);

            var keyCondition = TableQuery.GenerateFilterCondition("RowKey",
                                                                        QueryComparisons.Equal,
                                                                        resourceKey);

            var theCondition = TableQuery.CombineFilters(partitionCondition, TableOperators.And, keyCondition);
            var query = new TableQuery<LocalizationResourceEntity>().Where(theCondition);
            var table = GetTable();
            var result = table.ExecuteQuery(query);

            return FromEntity(result.FirstOrDefault());

            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand(@"
            //        SELECT
            //            r.Id,
            //            r.Author,
            //            r.FromCode,
            //            r.IsHidden,
            //            r.IsModified,
            //            r.ModificationDate,
            //            r.Notes,
            //            t.Id as TranslationId,
            //            t.Value as Translation,
            //            t.Language,
            //            t.ModificationDate as TranslationModificationDate
            //        FROM [dbo].[LocalizationResources] r
            //        LEFT JOIN [dbo].[LocalizationResourceTranslations] t ON r.Id = t.ResourceId
            //        WHERE ResourceKey = @key",
            //                             conn);
            //    cmd.Parameters.AddWithValue("key", resourceKey);

            //    var reader = cmd.ExecuteReader();

            //    if (!reader.Read())
            //    {
            //        return null;
            //    }

            //    var result = CreateResourceFromSqlReader(resourceKey, reader);

            //    // read 1st translation
            //    // if TranslationId is NULL - there is no translations for given resource
            //    if (!reader.IsDBNull(reader.GetOrdinal("TranslationId")))
            //    {
            //        result.Translations.Add(CreateTranslationFromSqlReader(reader, result));
            //        while (reader.Read())
            //        {
            //            result.Translations.Add(CreateTranslationFromSqlReader(reader, result));
            //        }
            //    }

            //    return result;
            //}
        }

        private LocalizationResource FromEntity(LocalizationResourceEntity firstOrDefault)
        {
            if (firstOrDefault == null)
            {
                throw new ArgumentNullException(nameof(firstOrDefault), "Entity is null.");
            }

            return new LocalizationResource(firstOrDefault.RowKey, _enableInvariantCultureFallback)
            {
                Author = firstOrDefault.Author,
                ModificationDate = firstOrDefault.ModificationDate,
                FromCode = firstOrDefault.FromCode,
                IsModified = firstOrDefault.IsModified,
                IsHidden = firstOrDefault.IsHidden
            };
        }

        /// <summary>
        /// Adds the translation for the resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="translation">The translation.</param>
        /// <exception cref="ArgumentNullException">
        /// resource
        /// or
        /// translation
        /// </exception>
        public void AddTranslation(LocalizationResource resource, LocalizationResourceTranslation translation)
        {
            //if (resource == null)
            //{
            //    throw new ArgumentNullException(nameof(resource));
            //}

            //if (translation == null)
            //{
            //    throw new ArgumentNullException(nameof(translation));
            //}

            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand(
            //        "INSERT INTO [dbo].[LocalizationResourceTranslations] ([Language], [ResourceId], [Value], [ModificationDate]) VALUES (@language, @resourceId, @translation, @modificationDate)",
            //        conn);
            //    cmd.Parameters.AddWithValue("language", translation.Language);
            //    cmd.Parameters.AddWithValue("resourceId", translation.ResourceId);
            //    cmd.Parameters.AddWithValue("translation", translation.Value);
            //    cmd.Parameters.AddWithValue("modificationDate", translation.ModificationDate);

            //    cmd.ExecuteNonQuery();
            //}
        }

        /// <summary>
        /// Updates the translation for the resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="translation">The translation.</param>
        /// <exception cref="ArgumentNullException">
        /// resource
        /// or
        /// translation
        /// </exception>
        public void UpdateTranslation(LocalizationResource resource, LocalizationResourceTranslation translation)
        {
            //if (resource == null)
            //{
            //    throw new ArgumentNullException(nameof(resource));
            //}

            //if (translation == null)
            //{
            //    throw new ArgumentNullException(nameof(translation));
            //}

            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand(
            //        "UPDATE [dbo].[LocalizationResourceTranslations] SET [Value] = @translation, [ModificationDate] = @modificationDate WHERE [Id] = @id",
            //        conn);
            //    cmd.Parameters.AddWithValue("translation", translation.Value);
            //    cmd.Parameters.AddWithValue("id", translation.Id);
            //    cmd.Parameters.AddWithValue("modificationDate", DateTime.UtcNow);

            //    cmd.ExecuteNonQuery();
            //}
        }

        /// <summary>
        /// Deletes the translation.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="translation">The translation.</param>
        /// <exception cref="ArgumentNullException">
        /// resource
        /// or
        /// translation
        /// </exception>
        public void DeleteTranslation(LocalizationResource resource, LocalizationResourceTranslation translation)
        {
            //if (resource == null)
            //{
            //    throw new ArgumentNullException(nameof(resource));
            //}

            //if (translation == null)
            //{
            //    throw new ArgumentNullException(nameof(translation));
            //}

            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand("DELETE FROM [dbo].[LocalizationResourceTranslations] WHERE [Id] = @id", conn);
            //    cmd.Parameters.AddWithValue("id", translation.Id);

            //    cmd.ExecuteNonQuery();
            //}
        }

        /// <summary>
        /// Updates the resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <exception cref="ArgumentNullException">resource</exception>
        public void UpdateResource(LocalizationResource resource)
        {
            //if (resource == null)
            //{
            //    throw new ArgumentNullException(nameof(resource));
            //}

            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand(
            //        "UPDATE [dbo].[LocalizationResources] SET [IsModified] = @isModified, [ModificationDate] = @modificationDate, [Notes] = @notes WHERE [Id] = @id",
            //        conn);
            //    cmd.Parameters.AddWithValue("id", resource.Id);
            //    cmd.Parameters.AddWithValue("modificationDate", resource.ModificationDate);
            //    cmd.Parameters.AddWithValue("isModified", resource.IsModified);
            //    cmd.Parameters.AddWithValue("notes", (object)resource.Notes ?? DBNull.Value);

            //    cmd.ExecuteNonQuery();
            //}
        }

        /// <summary>
        /// Deletes the resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <exception cref="ArgumentNullException">resource</exception>
        public void DeleteResource(LocalizationResource resource)
        {
            //if (resource == null)
            //{
            //    throw new ArgumentNullException(nameof(resource));
            //}

            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand("DELETE FROM [dbo].[LocalizationResources] WHERE [Id] = @id", conn);
            //    cmd.Parameters.AddWithValue("id", resource.Id);

            //    cmd.ExecuteNonQuery();
            //}
        }

        /// <summary>
        /// Deletes all resources. DANGEROUS!
        /// </summary>
        public void DeleteAllResources()
        {
            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand("DELETE FROM [dbo].[LocalizationResources]", conn);

            //    cmd.ExecuteNonQuery();
            //}
        }

        /// <summary>
        /// Inserts the resource in database.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <exception cref="ArgumentNullException">resource</exception>
        public void InsertResource(LocalizationResource resource)
        {
            //if (resource == null)
            //{
            //    throw new ArgumentNullException(nameof(resource));
            //}

            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand(
            //        "INSERT INTO [dbo].[LocalizationResources] ([ResourceKey], [Author], [FromCode], [IsHidden], [IsModified], [ModificationDate], [Notes]) OUTPUT INSERTED.ID VALUES (@resourceKey, @author, @fromCode, @isHidden, @isModified, @modificationDate, @notes)",
            //        conn);

            //    cmd.Parameters.AddWithValue("resourceKey", resource.ResourceKey);
            //    cmd.Parameters.AddWithValue("author", resource.Author ?? "unknown");
            //    cmd.Parameters.AddWithValue("fromCode", resource.FromCode);
            //    cmd.Parameters.AddWithValue("isHidden", resource.IsHidden);
            //    cmd.Parameters.AddWithValue("isModified", resource.IsModified);
            //    cmd.Parameters.AddWithValue("modificationDate", resource.ModificationDate);
            //    cmd.Parameters.AddSafeWithValue("notes", resource.Notes);

            //    // get inserted resource ID
            //    var resourcePk = (int)cmd.ExecuteScalar();

            //    // if there are also provided translations - execute those in the same connection also
            //    if (resource.Translations.Any())
            //    {
            //        foreach (var translation in resource.Translations)
            //        {
            //            cmd = new SqlCommand(
            //                "INSERT INTO [dbo].[LocalizationResourceTranslations] ([Language], [ResourceId], [Value], [ModificationDate]) VALUES (@language, @resourceId, @translation, @modificationDate)",
            //                conn);
            //            cmd.Parameters.AddWithValue("language", translation.Language);
            //            cmd.Parameters.AddWithValue("resourceId", resourcePk);
            //            cmd.Parameters.AddWithValue("translation", translation.Value);
            //            cmd.Parameters.AddWithValue("modificationDate", resource.ModificationDate);

            //            cmd.ExecuteNonQuery();
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Gets the available languages (reads in which languages translations are added).
        /// </summary>
        /// <param name="includeInvariant">if set to <c>true</c> [include invariant].</param>
        /// <returns></returns>
        public IEnumerable<CultureInfo> GetAvailableLanguages(bool includeInvariant)
        {
            return new[] { new CultureInfo("en") };

            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    conn.Open();

            //    var cmd = new SqlCommand(
            //        "SELECT DISTINCT [Language] FROM [dbo].[LocalizationResourceTranslations] WHERE [Language] <> ''",
            //        conn);
            //    var reader = cmd.ExecuteReader();

            //    var result = new List<CultureInfo>();
            //    if (includeInvariant)
            //    {
            //        result.Add(CultureInfo.InvariantCulture);
            //    }

            //    while (reader.Read())
            //    {
            //        result.Add(new CultureInfo(reader.GetString(0)));
            //    }

            //    return result;
            //}
        }

        /// <summary>
        ///Resets synchronization status of the resources.
        /// </summary>
        public void ResetSyncStatus()
        {
            //using (var conn = new SqlConnection(Settings.DbContextConnectionString))
            //{
            //    var cmd = new SqlCommand("UPDATE [dbo].[LocalizationResources] SET FromCode = 0", conn);

            //    conn.Open();
            //    cmd.ExecuteNonQuery();
            //    conn.Close();
            //}
        }

        /// <summary>
        ///Registers discovered resources.
        /// </summary>
        /// <param name="discoveredResources">Collection of discovered resources during scanning process.</param>
        /// <param name="allResources">All existing resources (so you could compare and decide what script to generate).</param>
        public void RegisterDiscoveredResources(
        ICollection<DiscoveredResource> discoveredResources,
            IEnumerable<LocalizationResource> allResources)
        {
            var table = GetTable();

            foreach (var discoveredResource in discoveredResources)
            {
                var existingResource = allResources.FirstOrDefault(r => r.ResourceKey == discoveredResource.Key);
                if (existingResource == null)
                {
                    table.Execute(TableOperation.InsertOrReplace(ToEntity(discoveredResource)));

                    foreach (var propertyTranslation in discoveredResource.Translations)
                    {
                        //table.Execute(TableOperation.InsertOrReplace(ToEntity()))
                    }
                }

                if (existingResource != null)
                {
                    if (existingResource.IsModified.HasValue && !existingResource.IsModified.Value)
                    {
                        foreach (var propertyTranslation in discoveredResource.Translations)
                        {
                        }
                    }
                }
            }



        //    // split work queue by 400 resources each
        //    var groupedProperties = discoveredResources.SplitByCount(400);

        //    Parallel.ForEach(groupedProperties,
        //                     group =>
        //                     {
        //                         var sb = new StringBuilder();
        //                         sb.AppendLine("DECLARE @resourceId INT");

        //                         var refactoredResources = group.Where(r => !string.IsNullOrEmpty(r.OldResourceKey));
        //                         foreach (var refactoredResource in refactoredResources)
        //                         {
        //                             sb.Append($@"
        //IF EXISTS(SELECT 1 FROM LocalizationResources WITH(NOLOCK) WHERE ResourceKey = '{refactoredResource.OldResourceKey}')
        //BEGIN
        //    UPDATE dbo.LocalizationResources SET ResourceKey = '{refactoredResource.Key}', FromCode = 1 WHERE ResourceKey = '{refactoredResource.OldResourceKey}'
        //END
        //");
        //                         }

        //                         foreach (var property in group)
        //                         {
        //                             var existingResource = allResources.FirstOrDefault(r => r.ResourceKey == property.Key);

        //                             if (existingResource == null)
        //                             {
        //                                 sb.Append($@"
        //SET @resourceId = ISNULL((SELECT Id FROM LocalizationResources WHERE [ResourceKey] = '{property.Key}'), -1)
        //IF (@resourceId = -1)
        //BEGIN
        //    INSERT INTO LocalizationResources ([ResourceKey], ModificationDate, Author, FromCode, IsModified, IsHidden)
        //    VALUES ('{property.Key}', GETUTCDATE(), 'type-scanner', 1, 0, {Convert.ToInt32(property.IsHidden)})
        //    SET @resourceId = SCOPE_IDENTITY()");

        //                                 // add all translations
        //                                 foreach (var propertyTranslation in property.Translations)
        //                                 {
        //                                     sb.Append($@"
        //    INSERT INTO LocalizationResourceTranslations (ResourceId, [Language], [Value], [ModificationDate]) VALUES (@resourceId, '{propertyTranslation.Culture}', N'{propertyTranslation.Translation.Replace("'", "''")}', GETUTCDATE())");
        //                                 }

        //                                 sb.Append(@"
        //END
        //");
        //                             }

        //                             if (existingResource != null)
        //                             {
        //                                 sb.AppendLine(
        //                                     $"UPDATE LocalizationResources SET FromCode = 1, IsHidden = {Convert.ToInt32(property.IsHidden)} where [Id] = {existingResource.Id}");

        //                                 var invariantTranslation = property.Translations.First(t => t.Culture == string.Empty);
        //                                 sb.AppendLine(
        //                                     $"UPDATE LocalizationResourceTranslations SET [Value] = N'{invariantTranslation.Translation.Replace("'", "''")}' where ResourceId={existingResource.Id} AND [Language]='{invariantTranslation.Culture}'");

        //                                 if (existingResource.IsModified.HasValue && !existingResource.IsModified.Value)
        //                                 {
        //                                     foreach (var propertyTranslation in property.Translations)
        //                                     {
        //                                         AddTranslationScript(existingResource, sb, propertyTranslation);
        //                                     }
        //                                 }
        //                             }
        //                         }

        //                         using (var conn = new SqlConnection(Settings.DbContextConnectionString))
        //                         {
        //                             var cmd = new SqlCommand(sb.ToString(), conn) { CommandTimeout = 60 };

        //                             conn.Open();
        //                             cmd.ExecuteNonQuery();
        //                             conn.Close();
        //                         }
        //                     });
        }

        private static CloudTable GetTable()
        {
            var storageAccount = CloudStorageAccount.Parse(Settings.ConnectionString);
            var client = storageAccount.CreateCloudTableClient();
            var table = client.GetTableReference("LocalizationResources");

            return table;
        }

        private LocalizationResourceEntity ToEntity(DiscoveredResource discoveredResource)
        {
            return new LocalizationResourceEntity(discoveredResource.Key)
            {
                Author = "type-scanner",
                ModificationDate = DateTime.UtcNow,
                FromCode = true,
                IsModified = false,
                IsHidden = discoveredResource.IsHidden
            };
        }

        private static void AddTranslationScript(
            LocalizationResource existingResource,
            StringBuilder buffer,
            DiscoveredTranslation resource)
        {
            var existingTranslation = existingResource.Translations.FirstOrDefault(t => t.Language == resource.Culture);
            if (existingTranslation == null)
            {
                buffer.Append($@"
        INSERT INTO [dbo].[LocalizationResourceTranslations] (ResourceId, [Language], [Value], [ModificationDate]) VALUES ({existingResource.Id}, '{resource.Culture}', N'{resource.Translation.Replace("'", "''")}', GETUTCDATE())");
            }
            else if (!existingTranslation.Value.Equals(resource.Translation))
            {
                buffer.Append($@"
        UPDATE [dbo].[LocalizationResourceTranslations] SET [Value] = N'{resource.Translation.Replace("'", "''")}' WHERE ResourceId={existingResource.Id} and [Language]='{resource.Culture}'");
            }
        }
    }
}