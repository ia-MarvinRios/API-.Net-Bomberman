using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace API.Documentation
{
    internal sealed class AuthOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;

            bool requiresAuth =
                metadata.OfType<IAuthorizeData>().Any() &&
                !metadata.OfType<IAllowAnonymous>().Any();

            if (!requiresAuth)
            {
                return Task.CompletedTask;
            }

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });

            return Task.CompletedTask;
        }
    }
}
