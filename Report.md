# Project Presentation

This is a continuation of the CSharpFL program started by Hanna Whitney.

## Additions I have made since taking over the project

### SBFLApp
* Separated the fault localization piece from the example C# application.
* Added additional suspiciousness calculations
  * Ochiai
  * DStar
  * Op2
  * Jaccard
* Added the generation of a CSV output or a markdown file
  * Shows the file, app name, class, and function along with a GUID associated with a single line of code
  * Gives a score for each of the 5 calculations
* Added the ability to pass in arguments for the solution, project, and test project to be evaluated
* Added arguments to specify the generated output
* Added unit tests

### MathApp
* Added additional mathmatical functions
* Added additional unit tests

## Results from running against [MathApp](https://github.com/hello84037/CSharpFL)
1. The error in the MathApp code is in the subtract method
```C#
// This method has extra code to test the SBFL app as well as an error.
public static int Subtract(int a, int b)
{
    // This if statement is extra to test the SBFL app
    if (a > b)
    {
        for (var i = 0; i < 1; i++)
        {
            int c = 0;
            c += a;
        }
    }
    int diff = a - b + 1; // Here is the error.
    return diff;
}
```
2. Command line arguments = {location of the MathApp solution file} MathApp.Tests MathApp --reset 
3. Run ./SBFLApp.exe {location of the MathApp solution file} MathApp.Tests MathApp --reset
4. The results are in the [mathapp_suspiciousness_report](mathapp_suspiciousness_report.csv)
5. Instrumentation statements are added to the Subtract method as shown below
```C#
public static int Subtract(int a, int b)
{
    System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "43ca7d6b-bfcc-42ce-8b7e-329bd4d012dd" + System.Environment.NewLine);
    if (a > b)
    {
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "578bb7c7-8a92-4303-bfd0-2df8a60f378c" + System.Environment.NewLine);
        for (var i = 0; i < 1; i++)
        {
            System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "f4add92e-b780-404e-8051-d33eead9527b" + System.Environment.NewLine);
            int c = 0;
            System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "fac2e41d-c110-4250-aed2-75c7cfd05141" + System.Environment.NewLine);
            c += a;
        }
    }

    System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "fc67a9e6-4512-487e-913a-1eb75e3353be" + System.Environment.NewLine);
    int diff = a - b + 1;
    System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "2de1557c-7d5b-40ec-8252-ff624ffc7a83" + System.Environment.NewLine);
    return diff;
}
```


## Results from running against [tdd-sample](https://github.com/mehdihadeli/tdd-sample)
1. The error in the tdd-sample code is in the GetTodoItemByIdHandler in the constructor
```C#
    public GetTodoItemByIdHandler(IRepository<Models.TodoItem> todoItemRepository, IMapper mapper)
    {
        _todoItemRepository = todoItemRepository;
        // Jake Child commented this out.
        //_mapper = mapper;
    }
```
2. Command line arguments = {location of the tdd-sample solution file} TDDSample.Tests TDDSample --reset
3. Run ./SBFLApp.exe {location of the tdd-sample solution file} TDDSample.Tests TDDSample --reset
4. The results are in the [tdd-sample_suspiciousness_report.csv](tdd-sample_suspiciousness_report.csv)
5. Instrumentation statements are added to the GetTodoItemByIdHandler method as shown below
```C#
    public GetTodoItemByIdHandler(IRepository<Models.TodoItem> todoItemRepository, IMapper mapper)
    {
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "583cee2b-096b-4caa-ba86-f27d040e664c" + System.Environment.NewLine);
        _todoItemRepository = todoItemRepository;
        // Jake Child commented this out.
        //_mapper = mapper;
    }
```

## Results from running against [CleanArch](https://github.com/sandeepkumar17/CleanArch/tree/master/CleanArch.Core/Entities)
1. The error in the CleanArch code is in the update method

```C#
[HttpPut]
public async Task<ApiResponse<string>> Update(Contact contact)
{
    var apiResponse = new ApiResponse<string>();
    try
    {
        var data = await _unitOfWork.Contacts.UpdateAsync(contact);
        apiResponse.Success = true;
        apiResponse.Result = data;
    }
    catch (SqlException ex)
    {
        apiResponse.Success = true; // Jake Child changed this from false to true
        apiResponse.Message = ex.Message;
        Logger.Instance.Error("SQL Exception:", ex);
    }
    catch (Exception ex)
    {
        System.IO.File.AppendAllText
        apiResponse.Success = false;
        System.IO.File.AppendAllText
        apiResponse.Message = ex.Message;
        System.IO.File.AppendAllText
        Logger.Instance.Error("Exception:", ex);
    }

    return apiResponse;
}
```

2. Command line arguments = {location of the CleanArch solution file} CleanArch.Tests CleanArch.Api --reset 
3. Run ./SBFLApp.exe {location of the CleanArch solution file} CleanArch.Tests CleanArch.Api --reset
4. The results are in the [cleanarch_suspiciousness_report.csv](cleanarch_suspiciousness_report.csv)
5. Instrumentation statements are added to the Update method as shown below

```C#
[HttpPut]
public async Task<ApiResponse<string>> Update(Contact contact)
{
    System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "947b45a2-3be9-49c2-b08b-b91a17f63279" + System.Environment.NewLine);
    var apiResponse = new ApiResponse<string>();
    System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "4c14d8c4-3b4c-48e0-b310-46adb833c52a" + System.Environment.NewLine);
    try
    {
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "328859e6-b32a-403f-aa31-1e1802c34bd9" + System.Environment.NewLine);
        var data = await _unitOfWork.Contacts.UpdateAsync(contact);
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "6adbc12f-da2c-4cfb-810b-a47ccfc351ed" + System.Environment.NewLine);
        apiResponse.Success = true;
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "27a0ac81-4aa0-4303-8ca4-45135ff1b927" + System.Environment.NewLine);
        apiResponse.Result = data;
    }
    catch (SqlException ex)
    {
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "9b067ad9-5a8b-471c-a802-0b3f19dc3f78" + System.Environment.NewLine);
        apiResponse.Success = true; // Jake Child changed this from false to true
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "900fcee1-cbcb-4a86-9a09-ec9f88df9830" + System.Environment.NewLine);
        apiResponse.Message = ex.Message;
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "289e9b13-e785-4875-b3c3-e52d8b7d9be5" + System.Environment.NewLine);
        Logger.Instance.Error("SQL Exception:", ex);
    }
    catch (Exception ex)
    {
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "338a4b17-4ec8-4d2b-a890-b4eba847ba4d" + System.Environment.NewLine);
        apiResponse.Success = false;
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "abbed15f-62b6-49d9-a156-398f56faecb0" + System.Environment.NewLine);
        apiResponse.Message = ex.Message;
        System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "a4e7e1f5-5747-45f9-8094-cac7ac760121" + System.Environment.NewLine);
        Logger.Instance.Error("Exception:", ex);
    }

    System.IO.File.AppendAllText("\\Coverage\\__sbfl_current_test.coverage.tmp", "a90626e1-d652-448e-a5ec-ec757159f0f1" + System.Environment.NewLine);
    return apiResponse;
}
```