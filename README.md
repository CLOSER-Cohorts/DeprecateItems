# DeprecateItems


This project contains all the componants for an MVC project using C# to Deprecate Items within a selected dataset or a number of datasets uploaded from a csv.

When the project loads there are two buttons. The left hand button allows the user to deprecate items. The right hand button displays information of variables with the selected dataset including the deprecated state.

On the top of the screen of the deprecate items there are two options. The first is where the user can select a single dataset to deprecate. The second allows the user to select the csv to upload of dataset

The format of input for datasets is 

AgencyId/Identifier

Deprecating a single dataset 

- Enter the URN and click on the Search button. This will display a summary of all the items based on Item type. C
- lick on the deprecate button on the base of the screen to deprecate all the items within this dataset.

Deprecating multiple datasets using a csv. 

- Select the csv of datasets to be deprecated.  
- Click on the Upload URN's button. This will display the list of datasets from the selected csv. Click on the Deprecate All button at the base of the screen to deprecate all items in the list of datasets

These will also reate a powershell script to be used to remove variables within dataset(s) from ElasticSearch. 
