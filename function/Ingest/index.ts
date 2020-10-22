import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const httpTrigger: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    context.log('Ingest function processed a request by ' + req.headers['x-ms-client-principal-name']);

    if (req.body == null) {
        context.res = {
            status: 400,
            body: "Invalid request"
        };
    }

    if (req.headers['X-MS-CLIENT-PRINCIPAL-ID'] != null) {
        let url:string = req.body.url;
        let shortUrl:string = req.body.shortUrl;
        let domain:string = req.body.domain
    
        context.bindings.shortUrl = [];
        context.bindings.shortUrl.push({
            PartitionKey: shortUrl,
            RowKey: domain,
            Url: url, 
            User: req.headers['X-MS-CLIENT-PRINCIPAL-ID']
        });
    
        context.done();   
    }
    else
    {
        context.res = {
            status: 401
        };
    }
};

export default httpTrigger;