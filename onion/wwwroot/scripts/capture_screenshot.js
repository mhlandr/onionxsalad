const puppeteer = require('puppeteer');

async function captureScreenshot(url, screenshotPath) {
    const browser = await puppeteer.launch({
        headless: true,
        args: [
            '--proxy-server=socks5://127.0.0.1:9050',
            '--no-sandbox'
        ]
    });

    try {
        const page = await browser.newPage();
        await page.setDefaultNavigationTimeout(120000);
        await page.goto(url, { waitUntil: 'networkidle2' });
        await page.screenshot({ path: screenshotPath, fullPage: true });
        console.log(`Screenshot saved at ${screenshotPath}`);
    } catch (error) {
        console.error(`Error accessing ${url}: ${error}`);
    } finally {
        await browser.close();
    }
}

// Capture command-line arguments
const args = process.argv.slice(2);
const url = args[0];
const screenshotPath = args[1];

captureScreenshot(url, screenshotPath);
