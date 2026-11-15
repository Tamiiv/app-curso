using Microsoft.AspNetCore.Identity;

namespace AppCurso.Web.Features.Account;

public static class IdentityErrorTranslator
{
    public static string Translate(IEnumerable<IdentityError> errors)
    {
        var messages = errors
            .Select(error => error.Code switch
            {
                "DuplicateUserName" or "DuplicateEmail" => "Ya existe una cuenta con ese email.",
                "InvalidUserName" or "InvalidEmail" => "El email no es válido.",
                "PasswordTooShort" => "La contraseña debe tener al menos 8 caracteres.",
                "PasswordRequiresNonAlphanumeric" => "La contraseña debe incluir al menos un símbolo.",
                "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número.",
                "PasswordRequiresLower" => "La contraseña debe incluir al menos una minúscula.",
                "PasswordRequiresUpper" => "La contraseña debe incluir al menos una mayúscula.",
                "PasswordMismatch" => "La contraseña actual no es correcta.",
                "InvalidToken" => "El enlace no es válido o ya venció. Pedí uno nuevo.",
                _ => error.Description
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return messages.Length == 0
            ? "No pudimos completar la operación. Intentá de nuevo."
            : string.Join(" ", messages);
    }
}
