# StyleVerse Hackathon: Tasks and Challenges

## Phase 1: The "Rusty" Rescue Mission

### Situation: Migrate or Perish

The viral success of StyleVerse has pushed our legacy on-premises server, affectionately known as "Rusty," to its absolute breaking point. The database is struggling to handle the daily influx of user data, resulting in performance bottlenecks and customer frustration.

The CTO has ordered an immediate migration to Azure, but with a strict requirement: the infrastructure must be repeatable and automated. Manual deployment in the portal is not allowed for primary resources. Your team must use Azure Bicep to define this new environment. However, a database without data is just an empty shell; you must also ensure the legacy StyleVerse product catalog is injected immediately so the website can go live and the business can stabilize.

### Artifacts

To deploy the application to the cloud, you’ll need access to the source code and the information required to run it locally:

- [GitHub repository: aider-app-mod-attendee](https://github.com/nephoseu/aider-app-mod-attendee)
- Make sure you notice the `.sql` script, which you’ll need for database initialization.

### Objectives & Tasks

To pass this phase and start earning points from the simulator, your team must satisfy the following:

- Provision an Azure SQL Database and an Azure App Service (Web App) using Bicep templates.
  - Use the resource group already created for you in your subscription.
  - Deploy your resources in the UK South region. If it is not available, deploy them in West Europe.
- Author a valid Bicep file that includes an App Service Plan, a Web App, and a SQL Server with a firewall rule configured to allow Azure services to access the database. Deploy the infrastructure directly via the Azure CLI.
- Populate the new Azure SQL database with the provided legacy schema (tables: `Users`, `Products`, `Orders`) and initial product data.
- Create a Bash or PowerShell script that automates execution of the SQL schema file against your new Azure endpoint using the Azure CLI.
- Configure the Web App with the correct connection strings to connect to the new database.

### Verification

- The scoring simulator will verify that the resources exist and are configured correctly in Azure.
- The simulator will ping the Web App’s `/api/products` endpoint. If the API returns an empty list or an error, the points for data seeding will not be awarded.

### Resources for the Mission

- Bicep Documentation: Quickstart: Create Azure SQL Database using Bicep
- Copilot & Bicep: Generate Bicep files using GitHub Copilot
- Execute `.sql` Script: `sqlcmd` utility
- App Service Config: Configure connection strings in Azure App Service

## Phase 2: The Relational Jailbreak

### Situation: Breaking the Relational Chains

StyleVerse’s CTO has identified Rusty’s legacy SQL database as the primary bottleneck preventing global expansion. To survive the next viral wave, the data infrastructure must transition to a cloud-native, NoSQL solution: Azure Cosmos DB. However, simply migrating tables to a NoSQL environment without any changes is a recipe for high costs and poor performance.

Your mission is to make a mental and technical shift. Move away from normalized SQL tables and design a document-oriented model where data that is “read together” is “stored together”. Investors are watching the scoring simulator; they want to see a schema built for high-speed retrieval at massive scale.

### Objectives & Tasks

To earn points in this phase, your team must accomplish the following:

- Provision an Azure Cosmos DB for NoSQL account and a container named `Products`.
- Analyze the existing SQL tables: `Products`, `Categories`, `Tags`, and `Details`.
- Design a single JSON document structure that represents a product, consolidating related tables into the document (embedding) to eliminate the need for relational joins.
- For the simulator, name the database `StyleVerseDb` and the container `Products`.
- Leverage GitHub Copilot to help map the relational logic into an optimized, nested JSON schema and identify the most effective partition key for the StyleVerse catalog.
- Migrate the existing catalog data from the SQL baseline into the new Cosmos DB container using migration tools or scripts.
- Optional: If time allows, migrate the `CartItems` and `Orders` tables.

### Verification

- The scoring simulator will check the Cosmos DB resource configuration and data integrity.
- The simulator will query a specific product item; points are awarded based on the efficiency of the JSON structure and the retrieval speed.

### Resources for the Mission

- NoSQL Theory: Data modeling in Azure Cosmos DB
- Transitioning: How to model and partition data on Azure Cosmos DB
- Migration Tools: Azure Cosmos DB Data Migration Tool
- Partitioning: Partitioning and horizontal scaling in Azure Cosmos DB

## Phase 3: Rewiring the Mainframe

### Situation: Cutting the SQL Cord

Now that your data has been successfully migrated to Cosmos DB, your application is experiencing a “language barrier.” The legacy code is still trying to speak “SQL” to a “NoSQL” database, resulting in errors and a broken storefront.

To transform StyleVerse into a modern powerhouse, your team must perform a high-stakes refactor. Rip out the old database access logic and replace it with the Azure Cosmos DB SDK. This is not just a search-and-replace task; ensure the code is efficient, asynchronous, and leverages the new data model designed in the previous phase.

### Objectives & Tasks

To earn points in this phase, your team must accomplish the following:

- Integrate the official Azure Cosmos DB SDK into the StyleVerse application project.
- Identify and remove legacy SQL connection strings, commands, and relational data mappers that are no longer compatible with the modernized infrastructure.
- Leverage GitHub Copilot to transform existing SQL-based data methods into modern Cosmos DB operations.
- Ensure queries use the partition key correctly to avoid expensive cross-partition queries, and implement asynchronous patterns to maintain application responsiveness.
- Successfully implement the logic to Create (Upsert) and Read fashion items from the `Products` container. When implementing the Read logic, the returned payload **must** include the `cosmosdb_id` field, populated with a unique identifier of the Cosmos DB document (`_rid` if using NoSQL, `objectid` or `_id` if using Mongo API).

### Verification

- The scoring simulator will send probe requests to your REST endpoints to verify that the application correctly interacts with Cosmos DB.
- Points are awarded for the successful retrieval of products and the proper implementation of NoSQL SDK patterns.

### Resources for the Mission

- Cosmos DB SDK Fundamentals: Best practices for Azure Cosmos DB .NET SDK
- Modernizing with AI: Refactoring code with GitHub Copilot Chat
- Querying in NoSQL: Get started with SQL queries in Azure Cosmos DB
- Asynchronous Patterns: Asynchronous programming with `async` and `await`

## Phase 4: Around the World in 80ms

### Situation: Going Global or Going Home

StyleVerse’s marketing campaign just went live in the USA and Asia, and the results are mixed. While domestic and neighbouring users are enjoying lightning-fast performance, customers in New York and Tokyo are complaining about “spinning circles” and slow checkout times. With investors pushing for rapid global expansion, the CTO knows that a single-region database is now a major liability.

To transform StyleVerse into a global fashion powerhouse, you must ensure that data is physically located closer to your users. Configure the infrastructure for global distribution so that a shopper in Paris and a shopper in New York can both experience sub-second response times.

### Objectives & Tasks

To secure your points for global scalability, your team must complete these two missions:

#### Mission A: The Data Foundation

- **Multi-Region Data Presence:** Configure your Azure Cosmos DB account to replicate the StyleVerse database across at least two additional Azure regions: East US 2 and East Asia.
- Leverage GitHub Copilot to modify your existing Bicep templates to support multi-region write or read locations. The updated infrastructure must reflect a multi-region topology that prioritizes high availability and disaster recovery.

#### Mission B: The Compute Expansion

- Deploy instances of the Web App in the same regions as your database (East US 2 and East Asia) by refactoring the App Service Bicep definition.
- Provision Azure Front Door to sit in front of the three apps and route traffic based on latency.
- Identify the necessary application-code changes to implement a Regional Connection Strategy. Ensure the application is configured to prefer local reads, preventing requests from crossing the ocean unnecessarily (for example, the App Service in North Europe should know to read from the North Europe database replica).

### Verification

- The scoring simulator will perform latency tests from different simulated global endpoints.
- Points are awarded based on the successful reduction of read latency and the correct configuration of the global consistency model.

### Resources for the Mission

- Global Distribution Concepts: Distribute your data globally with Azure Cosmos DB
- Front Door: Create a Front Door profile
- Bicep for Cosmos DB: Create an Azure Cosmos DB with multi-region replication using Bicep
- SDK Connectivity Best Practices: Azure Cosmos DB SDK connectivity modes and preferred regions
- Consistency Levels: Consistency levels in Azure Cosmos DB

## Phase 5: The Influencer Apocalypse

### Situation: The Flash Sale Frenzy

The moment of truth for StyleVerse has arrived. A global superstar wearing our flagship “StyleVerse Galaxy Hoodie” just posted a selfie to 100 million followers, and traffic is surging by 10,000%. This is a digital stampede that will test every optimization you have implemented.

The CTO is in the “War Room” watching live metrics. However, the bottleneck is not just the database—it is also the legacy code and the infrastructure’s ability to handle the load. If your configuration is not tuned, the high volume of requests will lead to timed-out orders (503 errors) and a total system collapse. Prove that the system can handle the demand by monitoring performance in real time, identifying bottlenecks, and scaling the system.

### Objectives & Tasks

- Navigate to the Insights or Metrics pane in the Azure Portal to monitor the live surge of traffic hitting Azure Front Door.
- Identify which API is being hit the most.
- **Scale the compute:** Configure Autoscale for your App Service Plans in all regions.
- **Scale the data:** Ensure the database can handle the peak by configuring Autoscale throughput on all targeted containers.
- **Perform an AI-powered code audit:** Use GitHub Copilot to analyze the code and identify performance bottlenecks.

### Verification

- The scoring simulator will ramp up requests to a massive peak level during this phase.
- Points are awarded for maintaining high availability and ensuring the system can respond to the simulator’s requests during peak load.

### Resources for the Mission

- Monitoring App Services: Monitor Azure App Service
- Monitoring Cosmos DB: Monitor Azure Cosmos DB data using Azure Monitor Log Analytics diagnostic settings
- Scaling out the compute: Automatic scaling in Azure App Service
- Database Scaling Strategies: Provision autoscale throughput on database or container
- Using GitHub Copilot for identifying performance bottlenecks: Improve code performance using GitHub Copilot Agent
