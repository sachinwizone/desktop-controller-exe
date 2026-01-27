import requests
import json

# Test the get_company_employees endpoint
url = 'http://localhost:8889/api.php?action=get_company_employees'
data = {
    'company_name': 'WIZONE IT NETWORK INDIA PVT LTD',
    'page': 1,
    'limit': 100
}

print("Testing get_company_employees...")
print(f"URL: {url}")
print(f"Data: {json.dumps(data, indent=2)}")

try:
    response = requests.post(url, json=data, timeout=10)
    print(f"\nStatus Code: {response.status_code}")
    print(f"Response: {json.dumps(response.json(), indent=2)}")
except Exception as e:
    print(f"Error: {e}")

# Also test get_departments
print("\n" + "="*50)
print("Testing get_departments...")
url2 = 'http://localhost:8889/api.php?action=get_departments'
data2 = {
    'company_name': 'WIZONE IT NETWORK INDIA PVT LTD'
}

print(f"URL: {url2}")
print(f"Data: {json.dumps(data2, indent=2)}")

try:
    response = requests.post(url2, json=data2, timeout=10)
    print(f"\nStatus Code: {response.status_code}")
    print(f"Response: {json.dumps(response.json(), indent=2)}")
except Exception as e:
    print(f"Error: {e}")
