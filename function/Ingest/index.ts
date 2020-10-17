import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const httpTrigger: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    context.log('Ingest function processed a request.');

    if (req.body == null) {
        context.res = {
            status: 400,
            body: "Invalid request"
        };
    }

    let url = req.body.Url;
    let shortUrl = req.body.ShortUrl


    context.bindings.shortUrl = [];
    context.bindings.shortUrl.push({
        PartitionKey: shortUrl,
        RowKey: 1,
        Url: url, 
        User: req.headers['x-ms-client-principal-name']
    });

    context.done();
};

export default httpTrigger;