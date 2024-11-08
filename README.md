.NET Onion Search Engine where users can search websites, check if a website is online, and capture a screenshot of the website.

When a search is made, the NoSQL database is queried for the search term, and any relevant results are returned to the user. Following this, a request is sent to Ahmia to scrape results for the search term. All results are checked against the database, and any new websites found are stored.

The project uses the TorSharp component to route traffic through the Tor network, allowing users to check if a website is online. A queue is implemented to manage website checks when multiple requests are made. A logging system is also in place to store all user requests in an SQL database.![userRequestSQL](https://github.com/user-attachments/assets/465f5c18-7770-4723-ab57-90a96fda9915)


For taking screenshots, a Node.js module called "Puppeteer" is used to capture images of the websites, which are then sent back to the user. A logging system is also implemented, which acts as a queue in SQL where the status and details about each request are displayed.
![userScreenshotQueueSQL](https://github.com/user-attachments/assets/90e7dcd6-c1bd-444f-bea1-dc7ff2ca2f63)
![userScreenshotSQL](https://github.com/user-attachments/assets/5d9c6370-d568-4643-a256-b2d87ef1d0a0)

Work in Progress
Other features to be implemented:

Link Saving: Add functionality to save links, along with comments and images related to those links.

Admin Panel: A panel where all searches are displayed to the admin.

Forensics Page: A page where users can download a website and examine image metadata for additional information.
