using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/formscore", async (Dictionary<string, string> data) =>
{
    string prompt = "On a scale from 1 to 10 what is the likelihood that the following form submission is spam with 1 being least likely spam and 10 being most likely spam. Answer only with a number 1 to 10.\n";
    SpamRating[] spamGrading = {
        SpamRating.Failed, 
        SpamRating.Not_Spam, SpamRating.Not_Spam, SpamRating.Not_Spam,              // 1-3
        SpamRating.Not_Likely_Spam, SpamRating.Not_Likely_Spam,                     // 4-5
        SpamRating.Likely_Spam, SpamRating.Likely_Spam, SpamRating.Likely_Spam,     // 6-8
        SpamRating.Spam, SpamRating.Spam,                                           // 9-10
    };

    int spamGrade = 0;

    // build a message containing the form fields provided as a dictionary
    string formContent = "";
    foreach (var (key, value) in data)
    {
        formContent += String.Format($"{key} = {value}\n");
    }
    Console.WriteLine($"{formContent}");

    // send the form fields to the LLM to get a spam rating
    var client = new ChatClient(model: "gpt-4o", apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    String message = prompt + formContent;
    Console.WriteLine($"[REQUEST]: {message}");
    ChatCompletion completion = await client.CompleteChatAsync(message);
    Console.WriteLine($"[RESPONSE]: {completion.Content[0].Text}");

    // get the resulting score from the response - return 0 if something goes wrong
    if (!int.TryParse(completion.Content[0].Text, out spamGrade))
    {
        spamGrade = 0;
    }
    spamGrade = (spamGrade > 10) ? 10 : spamGrade;
    return Results.Ok(new { Message = $"{spamGrading[spamGrade]}", Grade = spamGrade });

});

app.Run();

enum SpamRating
{
    Failed, Not_Spam, Not_Likely_Spam, Likely_Spam, Spam
};

