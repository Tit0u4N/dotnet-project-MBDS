namespace Gauniv.WebServer.Services;

using System.ComponentModel.DataAnnotations;

public static class ValidationHelper
{
    public static void Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(model, context, results, validateAllProperties: true))
        {
            var errors = results
                .Select(r => r.ErrorMessage)
                .Where(e => !string.IsNullOrEmpty(e));

            throw new ValidationException(string.Join(" | ", errors));
        }
    }
}
