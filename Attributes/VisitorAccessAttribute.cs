using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DeltaSocial.Attributes
{
    public class VisitorAccessAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Dacă utilizatorul are rolul de Visitor
            if (user.IsInRole("Visitor"))
            {
                // Verificăm dacă încercă să acceseze o acțiune restricționată
                var controller = context.RouteData.Values["controller"]?.ToString();
                var action = context.RouteData.Values["action"]?.ToString();

                // Lista de acțiuni permise pentru vizitatori
                var allowedActions = new Dictionary<string, string[]>
                {
                    { "Home", new[] { "Index", "Privacy" } },
                    { "Profile", new[] { "Index", "View" } }
                };

                // Verificăm dacă acțiunea este permisă
                if (!allowedActions.ContainsKey(controller) || 
                    !allowedActions[controller].Contains(action))
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                }
            }
        }
    }
} 