Feature: AddressAPI

A short summary of the feature

@AddAddressAPI
Scenario:01. Verify the Add Address API endpoint
	When User sends POST request on "Auth" base url with "SuperAdmin"
	Then User Add the address
	Then [outcome]
