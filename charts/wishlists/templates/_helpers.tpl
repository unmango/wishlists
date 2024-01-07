{{/* vim: set filetype=mustache: */}}

{{- define "wishlists.v1.api.image" -}}
{{ include "common.images.image" (dict "imageRoot" .Values.api.image "global" .Values.global) }}
{{- end -}}

{{- define "wishlists.v1.web.image" -}}
{{ include "common.images.image" (dict "imageRoot" .Values.web.image "global" .Values.global) }}
{{- end -}}

{{- define "wishlsits.v1.api.svc.headless" -}}
{{- printf "%s-hl" (include "common.names.fullname" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}
