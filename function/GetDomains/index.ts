import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const httpTrigger: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    context.log('GetDomain function processed a request.');

    context.res.status(200).json(context.bindings.configuration);
};

export default httpTrigger;