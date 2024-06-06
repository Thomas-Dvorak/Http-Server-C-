
# Http-Server-C# #

---

## Quick Summary ##

This project is a simple HTTP server made following the CodeCrafters guide. It was started the first day of this June, 2024. Because it was free that month, I decided to make one because I had wanted to anyway and it will look good on my resume. It taught me a lot about HTTP servers and how they communicate with other sources. I also learned about TCP (Transfer Control Protocol) and different methods like `GET` and `POST`. This was the base of this project. I plan on extending this HTTP server to be better and more efficient and have more features.

It also helped me get better at using C#.

---

## How To Use ##

As of now, the project can be run with the following steps:

1. Open Powershell.
2. Navigate to the directory where the project is located. (For me, it's `{your_user}/codecrafters-http-server`)
3. Run `dotnet run --project . --configuration Release -- "8.0"`. This command can be found in `your_server.sh`.
4. Open another Powershell window to access the server from.
5. Use `curl` commands to interact with the server. For more on `curl` commands, visit [Curl - Tutorial](https://curl.se/docs/tutorial.html). This is an example: `curl -v http://localhost:4221/echo/test`.

---

### To-Do ###

- [ ] Refactor the code from using `string` to `byte[]`.
- [ ] Open HTML files from `GET <file>`.
