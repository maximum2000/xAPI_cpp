#include <iostream>

#include <curl/curl.h>

#include <fstream>
#include <nlohmann/json.hpp>
using json = nlohmann::json;



int main()
{
	std::cout << "test" << std::endl;





    {
        //std::ifstream f("example.json");
       // json data = json::parse(f);

        // create an empty structure (null)
        json j;

        // add a number that is stored as double (note the implicit conversion of j to an object)
        j["pi"] = 3.141;

        // add a Boolean that is stored as bool
        j["happy"] = true;

        // add a string that is stored as std::string
        j["name"] = "Niels";

        // add another null object by passing nullptr
        j["nothing"] = nullptr;

        // add an object inside the object
        j["answer"]["everything"] = 42;

        // add an array that is stored as std::vector (using an initializer list)
        j["list"] = { 1, 0, 2 };

        // add another object (using an initializer list of pairs)
        j["object"] = { {"currency", "USD"}, {"value", 42.99} };

        // instead, you could also write (which looks very similar to the JSON above)
        json j2 = {
          {"pi", 3.141},
          {"happy", true},
          {"name", "Niels"},
          {"nothing", nullptr},
          {"answer", {
            {"everything", 42}
          }},
          {"list", {1, 0, 2}},
          {"object", {
            {"currency", "USD"},
            {"value", 42.99}
          }}
        };
    }












    std::string URL = "https://cloud.scorm.com/ScormEngineInterface/TCAPI/public/statements";
    std::string loginpassword = "test:test";
    std::string zzz = "{}";

    CURL* curl;
    struct curl_slist* headers = NULL;

    headers = curl_slist_append(headers, "Accept: application/json");
    headers = curl_slist_append(headers, "Content-Type: application/json");
    headers = curl_slist_append(headers, "X-Experience-API-Version:1.0.0");
    headers = curl_slist_append(headers, "charsets: utf-8");

    curl = curl_easy_init();

    if (curl)
    {
        /* enable verbose for easier tracing */
        curl_easy_setopt(curl, CURLOPT_VERBOSE, 1L);

        curl_easy_setopt(curl, CURLOPT_URL, URL.c_str());
        curl_easy_setopt(curl, CURLOPT_CUSTOMREQUEST, "POST"); //PUT
        curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
        //
        curl_easy_setopt(curl, CURLOPT_USERPWD, loginpassword.c_str()); //"test:test"
        // With the curl command line tool, you disable this with -k/--insecure.
        curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, false);
        curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, false);

        curl_easy_setopt(curl, CURLOPT_POST, 1);
        curl_easy_setopt(curl, CURLOPT_POSTFIELDS, zzz.c_str());

        std::cout << "..." << std::endl;
        CURLcode res = curl_easy_perform(curl);
        std::cout << std::endl << "..." << std::endl;

        /* Check for errors */
        if (res != CURLE_OK)
        {
            std::cout << "error:" << std::endl;
            fprintf(stderr, "curl_easy_perform() failed: %s\n", curl_easy_strerror(res));
            std::cout << std::endl;
        }

        curl_easy_cleanup(curl);
    }
    else
    {
        std::cout << "false" << std::endl;
    }


	return 0;
}