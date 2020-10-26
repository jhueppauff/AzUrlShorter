import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const httpTrigger: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    context.log('Redirect function processed a request. Short URL : ' + context.bindingData.shortUrl);

    var url:string;

    if (context.bindings.shortUrl == null) {
        // fallback
        url = "https://hueppauff.com/notfound";
    }
    else if (context.bindings.shortUrl.Url == null) {
        url = "https://hueppauff.com/notfound";
    } 
    else {
        url = context.bindings.shortUrl.Url;
    }

    context.res = {
        status: 302,
        headers: {
            "location": url
        },
        body: null
    };
};

export default httpTrigger;