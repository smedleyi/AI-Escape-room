-- One JSON document per cart session, with product name and price copied into each item.
SET NOCOUNT ON;
SELECT (
    SELECT
        s.SessionId AS id,
        s.SessionId AS sessionId,
        'cart' AS type,
        (
            SELECT
                ci.ProductId AS productId,
                p.Name AS name,
                p.Price AS price,
                ci.Quantity AS quantity,
                ci.DateAdded AS dateAdded
            FROM CartItems ci
            JOIN Products p ON p.Id = ci.ProductId
            WHERE ci.SessionId = s.SessionId
            ORDER BY ci.Id
            FOR JSON PATH
        ) AS items
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
)
FROM (SELECT DISTINCT SessionId FROM CartItems) AS s
ORDER BY s.SessionId;
