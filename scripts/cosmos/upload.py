"""Upsert JSON-lines documents into a Cosmos DB container. Safe to re-run."""
import json
import os
import sys

from azure.cosmos import CosmosClient


def main() -> None:
    if len(sys.argv) != 3:
        sys.exit("usage: upload.py <container> <file.jsonl>")
    container_name, path = sys.argv[1], sys.argv[2]

    client = CosmosClient(os.environ["COSMOS_ENDPOINT"], credential=os.environ["COSMOS_KEY"])
    container = client.get_database_client(os.environ.get("COSMOS_DATABASE", "StyleVerseDb")).get_container_client(container_name)

    count = 0
    with open(path, encoding="utf-8") as f:
        for line in f:
            if line.strip():
                container.upsert_item(json.loads(line))
                count += 1
    print(f"{container_name}: upserted {count} documents")


if __name__ == "__main__":
    main()
