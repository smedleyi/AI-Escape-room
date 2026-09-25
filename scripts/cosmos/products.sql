-- One JSON document per product: category and tags embedded, so a product needs no joins.
SET NOCOUNT ON;
SELECT (
    SELECT
        CAST(p.Id AS NVARCHAR(20)) AS id,
        'product' AS type,
        p.Name AS name,
        p.Description AS description,
        p.Price AS price,
        p.InventoryCount AS inventoryCount,
        p.CreatedDate AS createdDate,
        c.Id AS [category.id],
        c.Name AS [category.name],
        JSON_QUERY(ISNULL((
            SELECT '[' + STRING_AGG('"' + STRING_ESCAPE(t.Name, 'json') + '"', ',') WITHIN GROUP (ORDER BY t.Name) + ']'
            FROM ProductTags pt
            JOIN Tags t ON t.Id = pt.TagId
            WHERE pt.ProductId = p.Id
        ), '[]')) AS tags
    FROM (SELECT 1 AS x) AS one
    LEFT JOIN Categories c ON c.Id = p.CategoryId
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
)
FROM Products p
ORDER BY p.Id;
