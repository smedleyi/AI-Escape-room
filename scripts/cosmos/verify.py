"""Checks migrated Cosmos data: counts, a sample product, and point-read cost and latency."""
import json
import os
import time

from azure.cosmos import CosmosClient

client = CosmosClient(os.environ["COSMOS_ENDPOINT"], credential=os.environ["COSMOS_KEY"])
db = client.get_database_client("StyleVerseDb")

for name in ("Products", "Carts", "Orders"):
    c = db.get_container_client(name)
    count = list(c.query_items("SELECT VALUE COUNT(1) FROM c", enable_cross_partition_query=True))[0]
    pk = c.read()["partitionKey"]["paths"][0]
    print(f"{name}: {count} docs, partition key {pk}")

products = db.get_container_client("Products")
products.read_item("28", partition_key="28")  # warm up the connection
start = time.perf_counter()
doc = products.read_item("28", partition_key="28")
ms = (time.perf_counter() - start) * 1000
ru = products.client_connection.last_response_headers["x-ms-request-charge"]
print(f"Point read id=28: {ms:.0f} ms, {ru} RU")
print(json.dumps({k: v for k, v in doc.items() if not k.startswith("_")}, indent=2))

for o in db.get_container_client("Orders").query_items("SELECT c.id, c.email, c.customerName, c.totalAmount, c.orderDate, ARRAY_LENGTH(c.items) AS lines FROM c", enable_cross_partition_query=True):
    print("order:", o)
