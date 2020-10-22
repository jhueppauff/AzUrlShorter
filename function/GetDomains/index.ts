import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const httpTrigger: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    context.log('GetDomain function processed a request.');

    if (req.headers['X-MS-CLIENT-PRINCIPAL-ID'] != null) {
        context.res.status(200).json(context.bindings.configuration);   
    }
    else
    {
        context.res = {
            status: 401
        };
    }
};

export default httpTrigger;