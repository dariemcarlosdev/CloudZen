using CloudZen.Features.Contact.Models;
using CloudZen.Features.Contact.Services;
using Microsoft.AspNetCore.Components;

namespace CloudZen.Features.Contact.Components;

/// <summary>
/// Code-behind for ContactForm.razor — handles form state and email submission.
/// </summary>
public partial class ContactForm
{
    [Inject] private IEmailService EmailService { get; set; } = default!;

    private ContactFormModel formModel = new();
    private bool submitted;
    private bool isSubmitting;
    private string? errorMessage;

    private async Task HandleValidSubmit()
    {
        isSubmitting = true;
        errorMessage = null;

        try
        {
            var result = await EmailService.SendEmailAsync(
                formModel.Subject!,
                formModel.Message!,
                formModel.Name!,
                formModel.Email!
            );

            if (result.Success)
            {
                submitted = true;
            }
            else
            {
                errorMessage = result.Error ?? "Failed to send message. Please try again.";
            }
        }
        catch (Exception)
        {
            errorMessage = "An unexpected error occurred. Please try again later.";
        }
        finally
        {
            isSubmitting = false;
        }
    }

    private void ResetForm()
    {
        formModel = new ContactFormModel();
        submitted = false;
        errorMessage = null;
    }
}
