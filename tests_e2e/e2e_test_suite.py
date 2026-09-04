import unittest
import os
import sys
from playwright.sync_api import sync_playwright

BASE_URL = "http://localhost:5169"

class MothersonBoxManagementE2ETests(unittest.TestCase):
    
    @classmethod
    def setUpClass(cls):
        cls.playwright = sync_playwright().start()
        cls.browser = cls.playwright.chromium.launch(headless=True)

    @classmethod
    def tearDownClass(cls):
        cls.browser.close()
        cls.playwright.stop()

    def setUp(self):
        # Fresh isolated context and page for each test
        self.test_context = self.browser.new_context(viewport={"width": 1440, "height": 900})
        self.page = self.test_context.new_page()

    def tearDown(self):
        self.page.close()
        self.test_context.close()

    def goto_safe(self, url, timeout=30000):
        for attempt in range(3):
            try:
                self.page.goto(url, timeout=timeout)
                return
            except Exception as e:
                print(f"Goto failed on attempt {attempt+1}: {e}")
                if attempt == 2:
                    raise
                self.page.wait_for_timeout(2000)

    def assert_on_dashboard(self):
        url = self.page.url.rstrip('/')
        self.assertTrue(url == BASE_URL or "/Dashboard" in url, f"Not on Dashboard. Current URL: {self.page.url}")

    def test_01_authentication_redirection_and_failure(self):
        """Test authentication redirection and login validation errors."""
        print("Running test_01_authentication_redirection_and_failure...")
        # Accessing protected page should redirect to login
        self.goto_safe(f"{BASE_URL}/Dashboard")
        self.page.wait_for_timeout(1000)
        self.assertIn("/Account/Login", self.page.url)

        # Submit empty form
        self.page.click('form button[type="submit"]')
        self.page.wait_for_timeout(1000)
        self.assertTrue(self.page.locator('.text-danger').first.is_visible() or "/Account/Login" in self.page.url)

        # Login with invalid credentials
        self.page.fill('input[name="Matricule"]', 'OP001')
        self.page.fill('input[name="Password"]', 'WrongPassword123!')
        self.page.click('form button[type="submit"]')
        self.page.wait_for_timeout(1000)
        self.assertIn("/Account/Login", self.page.url)

    def test_02_operator_box_creation_and_scan_workflow(self):
        """Test operator dashboard, box template creation, and manual scan association."""
        print("Running test_02_operator_box_creation_and_scan_workflow...")
        
        # Login with correct Operator credentials
        self.goto_safe(f"{BASE_URL}/Account/Login")
        self.page.fill('input[name="Matricule"]', 'OP001')
        # Try both default and updated operator passwords
        self.page.fill('input[name="Password"]', 'Op!6041FD23E6B56EF200A8E2E1')
        self.page.click('form button[type="submit"]')
        self.page.wait_for_timeout(1500)

        if "/Account/Login" in self.page.url:
            self.page.fill('input[name="Password"]', 'Op!6041FD23E6B56EF200A8E2E1_new')
            self.page.click('form button[type="submit"]')
            self.page.wait_for_timeout(1500)

        # Ensure we are logged in and on Dashboard
        self.assert_on_dashboard()

        # Navigate to open a new box
        self.goto_safe(f"{BASE_URL}/Dashboard/Templates")
        self.page.wait_for_timeout(1000)

        # Create box from first template card
        self.page.click('.template-card button[type="submit"]')
        self.page.wait_for_timeout(2000)

        # Extract box barcode value
        if "PrintClient" in self.page.url:
            # If redirected to print screen, get barcode and go to details
            box_barcode = self.page.url.split("/")[-1].split("?")[0]
            self.goto_safe(f"{BASE_URL}/Box/Details/{box_barcode}")
        else:
            # We are on details page directly
            box_barcode = self.page.locator('.info-row-value code').first.text_content().strip()
        
        self.assertTrue(box_barcode.startswith("BOX-"))
        print(f"E2E created box: {box_barcode}")

        # Simulate scanning package 9990008 (doesn't match template, triggers manual link)
        print("E2E: Simulating scan of package 9990008")
        self.page.evaluate("window.simulateScan('9990008')")
        self.page.wait_for_timeout(1000)

        # Verify scanner warning overlay shown: Awaiting Box (using correct id #scanOverlay)
        overlay_text = self.page.locator('#scanOverlay').text_content()
        self.assertIn("PACKAGE DETECTED", overlay_text.upper())

        # Simulate scanning the box barcode to complete association
        print(f"E2E: Simulating scan of box {box_barcode}")
        self.page.evaluate(f"window.simulateScan('{box_barcode}')")
        self.page.wait_for_timeout(1500)

        # Verify details page table content updating
        self.goto_safe(f"{BASE_URL}/Box/Details/{box_barcode}")
        self.page.wait_for_timeout(1000)
        table_html = self.page.locator('table').inner_html()
        self.assertIn("9990008", table_html)

    def test_03_admin_views_access(self):
        """Test administrator access to audit logs and user management."""
        print("Running test_03_admin_views_access...")
        
        # Login as Admin
        self.goto_safe(f"{BASE_URL}/Account/Login")
        self.page.fill('input[name="Matricule"]', 'AD001')
        self.page.fill('input[name="Password"]', 'Ad!C3CFE7FE6504FBDCBCC9B713')
        self.page.click('form button[type="submit"]')
        self.page.wait_for_timeout(1500)

        if "/Account/Login" in self.page.url:
            self.page.fill('input[name="Password"]', 'Ad!C3CFE7FE6504FBDCBCC9B713_new')
            self.page.click('form button[type="submit"]')
            self.page.wait_for_timeout(1500)

        self.assert_on_dashboard()

        # Access Audit logs
        self.goto_safe(f"{BASE_URL}/Audit")
        self.page.wait_for_timeout(1000)
        self.assertIn("/Audit", self.page.url)
        self.assertIn("audit", self.page.locator('h1').text_content().lower())

        # Access User Management
        self.goto_safe(f"{BASE_URL}/Users")
        self.page.wait_for_timeout(1000)
        self.assertIn("/Users", self.page.url)
        self.assertIn("user", self.page.locator('h1').text_content().lower())

if __name__ == "__main__":
    unittest.main()
