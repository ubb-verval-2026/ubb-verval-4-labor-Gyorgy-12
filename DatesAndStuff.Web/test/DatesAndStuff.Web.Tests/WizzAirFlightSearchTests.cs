using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace DatesAndStuff.Web.Tests;

[TestFixture]
public class WizzAirFlightSearchTests
{
    private IWebDriver _driver;
    private const string WizzAirUrl = "https://wizzair.com/en-gb";

    [SetUp]
    public void SetUp()
    {
        var options = new ChromeOptions();
        // Add arguments to help bypass basic bot detection and run smoother
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument("--start-maximized");
        options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36");
        
        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    [TearDown]
    public void TearDown()
    {
        if (_driver != null)
        {
            _driver.Quit();
            _driver.Dispose();
        }
    }

    [Test]
    public void SearchBucharestToBudapest_NextWeek_ShouldFindAtLeastTwoFlights()
    {
        // 1. Arrange dates (next week from today)
        var today = DateTime.Now;
        var nextWeekStart = today.AddDays(7);
        
        _driver.Navigate().GoToUrl(WizzAirUrl);
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));

        try
        {
            // 2. Accept cookies if present
            try 
            {
                var cookieAccept = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("//*[contains(text(), 'Accept all') or contains(text(), 'ACCEPT ALL')]")));
                cookieAccept.Click();
                Thread.Sleep(1000);
            }
            catch (WebDriverTimeoutException) { /* No cookie banner */ }

            // 3. Select Departure (Bucharest)
            var originInput = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("search-departure-station")));
            originInput.SendKeys(Keys.Control + "a");
            originInput.SendKeys(Keys.Backspace);
            originInput.SendKeys("Bucharest");
            Thread.Sleep(1000);
            
            var originOption = wait.Until(ExpectedConditions.ElementToBeClickable(
                By.XPath("//mark[contains(text(), 'Bucharest')]|//strong[contains(text(), 'Bucharest')]|//span[contains(text(), 'Bucharest')]")));
            originOption.Click();

            // 4. Select Destination (Budapest)
            var destinationInput = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("search-arrival-station")));
            destinationInput.SendKeys(Keys.Control + "a");
            destinationInput.SendKeys(Keys.Backspace);
            destinationInput.SendKeys("Budapest");
            Thread.Sleep(1000);

            var destOption = wait.Until(ExpectedConditions.ElementToBeClickable(
                By.XPath("//mark[contains(text(), 'Budapest')]|//strong[contains(text(), 'Budapest')]|//span[contains(text(), 'Budapest')]")));
            destOption.Click();

            // 5. Select Date (Next week)
            var datePicker = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("search-departure-date")));
            datePicker.Click();
            Thread.Sleep(1000);

            // WizzAir calendar can be tricky, find the specific day button
            var dayToSelect = nextWeekStart.Day.ToString();
            var dayButtonXPath = $"//button[not(@disabled) and contains(@class, 'calendar-day') and .//span[text()='{dayToSelect}']]";
            
            var dayElements = wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath(dayButtonXPath)));
            // Usually there are two calendars side by side, prefer the one that matches our logic or just the first available one
            dayElements.FirstOrDefault()?.Click();
            
            // Confirm date selection
            var okButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[@data-test='calendar-shrink-btn']")));
            okButton.Click();

            // 6. Click Search
            var searchButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[@data-test='flight-search-submit']")));
            searchButton.Click();

            // 7. Wait for results and assert
            // WizzAir shows flight cards with generic classes, we wait for the flight list container
            var flightCards = wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(
                By.XPath("//div[@data-test='flight-select-flight-info']|//div[contains(@class, 'flight-card')]")));
            
            flightCards.Count.Should().BeGreaterThanOrEqualTo(2, 
                $"we expect at least 2 flights from Bucharest to Budapest next week (searched for {nextWeekStart:yyyy-MM-dd}).");
        }
        catch (WebDriverTimeoutException ex)
        {
            Assert.Fail($"Test failed due to timeout while waiting for elements. URL: {_driver.Url}. Error: {ex.Message}");
        }
        catch (NoSuchElementException ex)
        {
            Assert.Fail($"Test failed because an element could not be found. URL: {_driver.Url}. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Test failed unexpectedly. Error: {ex.Message}");
        }
    }
}
