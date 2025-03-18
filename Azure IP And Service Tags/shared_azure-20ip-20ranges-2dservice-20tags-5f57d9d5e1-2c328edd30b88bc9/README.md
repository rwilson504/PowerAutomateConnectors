# Azure IP Ranges - Service Tags

Azure IP Ranges - Service Tags is a connector that enables you to retrieve and process Azure IP ranges and service tag information. With this connector, you can generate direct download URLs, extract service tags, fetch complete file contents, filter IP addresses by service tag and IP version, reduce CIDR blocks based on a maximum prefix, and generate IP rules. These operations allow you to integrate Azure networking details into your workflows seamlessly.

## Publisher: Richard Wilson

## Prerequisites

There are no pre-requisites for this connector.

## Supported Operations

The Azure IP Ranges - Service Tags connector supports the following operations:

### 1. Get Direct Download URL

Retrieves the direct download URL based on an input ID corresponding to specific cloud environments.

- **Input Properties:**
  - **ID:** Options include "Public Cloud", "US Government", or "China Cloud".
- **Output:**
  - **Download URL:** The URL where the file can be directly downloaded.

### 2. Get Service Tags

Fetches service tag information from a downloaded file.

- **Input Properties:**
  - **Download URL:** The URL of the file containing service tag data.
- **Output:**
  - **Download URL:** The content containing the service tags.

### 3. Get All File Contents

Retrieves all contents from a given download URL.

- **Input Properties:**
  - **Download URL:** The URL to fetch complete file contents.
- **Output:**
  - **Download URL:** The file content as returned by the download source.

### 4. Get IP Addresses By Service Tag

Extracts IP addresses corresponding to a specified service tag. You can also filter by IP version (All, IPv4, or IPv6).

- **Input Properties:**
  - **Download URL:** URL to retrieve the file content.
  - **Service Tag:** The name of the service tag to filter by.
  - **IP Version:** Filter by "All", "IPv4" (default), or "IPv6".
- **Output:**
  - **IP Addresses:** A list of IP addresses matching the criteria.
  - **Count:** The total number of IP addresses returned.

### 5. Reduce CIDR Values

Reduces a list of provided CIDR values by eliminating overlapping ranges and adjusting based on a specified maximum prefix.

- **Input Properties:**
  - **CIDR Values:** An array of CIDR values.
  - **Max Prefix:** The maximum allowed prefix length.
- **Output:**
  - **Reduced Values:** A distinct list of CIDR values after reduction.
  - **Original Count:** The count of the original CIDR values.
  - **Reduced Count:** The count of the CIDR values after reduction.

### 6. Generate IP Rules

Generates IP rules by pairing each input IP address or CIDR value with a specified rule action (Allow or Deny).

- **Input Properties:**
  - **IP List:** An array of IP addresses or CIDR values.
  - **Action:** The rule action, either Allow or Deny (default is Allow).
- **Output:**
  - An array of objects, where each object contains:
    - **Value:** The IP address or CIDR.
    - **Action:** The associated rule action.

## Obtaining Credentials

The connector does not require any authentication.

## Known Issues and Limitations

There are currently no known issues or limitations. Please refer to the latest documentation for updates regarding compatibility and performance.
