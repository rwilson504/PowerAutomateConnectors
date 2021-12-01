### NOTE
> This is a *sample* connector.  The connector is provided here with the intent to illustrate certain features and functionality around connectors.

## Exchange Online Contacts - Microsoft Power Automate Connector
Utilizes the Graph API to connect to Exchange contact folders. Allows you to access contact folders that you have delegation access to.  Also allows for create/update/delete of contacts.

## Prerequisites
You will need to create an App registration that provides the following API Permissions
| API Permission name| Type | Notes |
|---|---|---|
|Contacts.Read|Delegated|
|Contacts.ReadWrite|Delegated|
|Contacts.Read.Shared|Delegated|Only needed if you want to access contact lists that are delegated.|
|Contacts.ReadWrite.Shared|Delegated|Only needed if you want to access contact lists that are delegated.|

## Triggers

* none

## Actions

* GetContactFolders: Get the list of contacts folders based upon a the Office 365 User Id.
* GetContacts: Gets the contacts for a specific contact folder.
* DeleteContact: Delete a contact
* UpdateContact: Update a contact
* CreateContact: Create a contact
