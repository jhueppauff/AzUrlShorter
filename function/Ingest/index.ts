import { AzureFunction, Context, HttpRequest } from "@azure/functions"
import rp = require('request-promise');

const httpTrigger: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    context.log('Ingest function processed a request by ' + req.headers['x-ms-client-principal-name']);

    if (req.body == null) {
        context.res = {
            status: 400,
            body: "Invalid request"
        };
    }

    let url:string = req.body.url;
    let shortUrl:string = req.body.shortUrl;

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