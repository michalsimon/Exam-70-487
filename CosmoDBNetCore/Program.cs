﻿namespace CosmoDBNetCore
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    using Newtonsoft.Json;

    class Program
    {
        private const string EndpointUri = "https://ms-test-cosmo-db.documents.azure.com:443/";

        private const string PrimaryKey = "RZj8O155ffjL4F2koHNNgrvePlKuGBAfrbmLB9JL6RPOfJ3Rc3xUHTesS0d5KzGC4EjQ62OuQnATZpHVAIWFag==";

        private DocumentClient client;

        static void Main(string[] args)
        {
            try
            {
                Program p = new Program();
                p.GetStartedDemo().Wait();
            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        private async Task GetStartedDemo()
        {
            this.client = new DocumentClient(new Uri(EndpointUri), PrimaryKey);
            await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = "FamilyDB_oa" });

            this.client = new DocumentClient(new Uri(EndpointUri), PrimaryKey);

            await this.client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri("FamilyDB_oa"),
                new DocumentCollection { Id = "FamilyCollection_oa" });

            Family andersenFamily = new Family
            {
                Id = "Andersen.1",
                LastName = "Andersen",
                Parents = new Parent[]
                          {
                              new Parent { FirstName = "Thomas" },
                              new Parent { FirstName = "Mary Kay" }
                          },
                Children = new Child[]
                           {
                               new Child
                               {
                                   FirstName = "Henriette Thaulow",
                                   Gender = "female",
                                   Grade = 5,
                                   Pets = new Pet[]
                                          { new Pet { GivenName = "Fluffy" } }
                               }
                           },
                Address = new Address { State = "WA", County = "King", City = "Seattle" },
                IsRegistered = true
            };

            await this.CreateFamilyDocumentIfNotExists("FamilyDB_oa", "FamilyCollection_oa", andersenFamily);

            Family wakefieldFamily = new Family
            {
                Id = "Wakefield.7",
                LastName = "Wakefield",
                Parents = new Parent[]
                          {
                              new Parent { FamilyName = "Wakefield", FirstName = "Robin" },
                              new Parent { FamilyName = "Miller", FirstName = "Ben" }
                          },
                Children = new Child[]
                           {
                               new Child
                               {
                                   FamilyName = "Merriam",
                                   FirstName = "Jesse",
                                   Gender = "female",
                                   Grade = 8,
                                   Pets = new Pet[]
                                          {
                                              new Pet { GivenName = "Goofy" },
                                              new Pet { GivenName = "Shadow" }
                                          }
                               },
                               new Child
                               {
                                   FamilyName = "Miller",
                                   FirstName = "Lisa",
                                   Gender = "female",
                                   Grade = 1
                               }
                           },
                Address = new Address { State = "NY", County = "Manhattan", City = "NY" },
                IsRegistered = false
            };

            await this.CreateFamilyDocumentIfNotExists("FamilyDB_oa", "FamilyCollection_oa", wakefieldFamily);

            this.ExecuteSimpleQuery("FamilyDB_oa", "FamilyCollection_oa");

            // Update the Grade of the Andersen Family child
            andersenFamily.Children[0].Grade = 6;

            await this.ReplaceFamilyDocument("FamilyDB_oa", "FamilyCollection_oa", "Andersen.1", andersenFamily);

            this.ExecuteSimpleQuery("FamilyDB_oa", "FamilyCollection_oa");

            // Delete Andersen Family
            await this.DeleteFamilyDocument("FamilyDB_oa", "FamilyCollection_oa", "Andersen.1");

            // Clean up/delete the database
            await this.client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri("FamilyDB_oa"));
        }

        private async Task CreateFamilyDocumentIfNotExists(string databaseName, string collectionName, Family family)
        {
            try
            {
                await this.client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, family.Id));
                this.WriteToConsoleAndPromptToContinue("Found {0}", family.Id);
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), family);
                    this.WriteToConsoleAndPromptToContinue("Created Family {0}", family.Id);
                }
                else
                {
                    throw;
                }
            }
        }

        private void ExecuteSimpleQuery(string databaseName, string collectionName)
        {
            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Here we find the Andersen family via its LastName
            IQueryable<Family> familyQuery = this.client.CreateDocumentQuery<Family>(
                    UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                    queryOptions)
                .Where(f => f.LastName == "Andersen");

            // The query is executed synchronously here, but can also be executed asynchronously via the IDocumentQuery<T> interface
            Console.WriteLine("Running LINQ query...");
            foreach (Family family in familyQuery)
            {
                Console.WriteLine("\tRead {0}", family);
            }

            // Now execute the same query via direct SQL
            IQueryable<Family> familyQueryInSql = this.client.CreateDocumentQuery<Family>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                "SELECT * FROM Family WHERE Family.LastName = 'Andersen'",
                queryOptions);

            Console.WriteLine("Running direct SQL query...");
            foreach (Family family in familyQueryInSql)
            {
                Console.WriteLine("\tRead {0}", family);
            }

            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey();
        }

        private async Task ReplaceFamilyDocument(string databaseName, string collectionName, string familyName, Family updatedFamily)
        {
            await this.client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, familyName), updatedFamily);
            this.WriteToConsoleAndPromptToContinue("Replaced Family {0}", familyName);
        }

        private async Task DeleteFamilyDocument(string databaseName, string collectionName, string documentName)
        {
            await this.client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, documentName));
            Console.WriteLine("Deleted Family {0}", documentName);
        }

        private void WriteToConsoleAndPromptToContinue(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey();
        }

        public class Family
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }

            public string LastName { get; set; }

            public Parent[] Parents { get; set; }

            public Child[] Children { get; set; }

            public Address Address { get; set; }

            public bool IsRegistered { get; set; }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public class Parent
        {
            public string FamilyName { get; set; }

            public string FirstName { get; set; }
        }

        public class Child
        {
            public string FamilyName { get; set; }

            public string FirstName { get; set; }

            public string Gender { get; set; }

            public int Grade { get; set; }

            public Pet[] Pets { get; set; }
        }

        public class Pet
        {
            public string GivenName { get; set; }
        }

        public class Address
        {
            public string State { get; set; }

            public string County { get; set; }

            public string City { get; set; }
        }
    }
}