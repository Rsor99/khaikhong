export interface ValidationErrorDetail {
  field: string;
  error: string;
}

export interface GeneralErrorDetail {
  message: string;
  traceId?: string;
}

export type ApiResponseErrors =
  | ValidationErrorDetail[]
  | GeneralErrorDetail
  | null;

export interface BaseApiResponse<T> {
  status: number;
  message: string;
  isSuccess: boolean;
  data: T | null;
  errors: ApiResponseErrors;
}

export interface ApiSuccessObjResponse<T> extends BaseApiResponse<T> {
  isSuccess: true;
  data: T;
  errors: null;
}

export type ApiSuccessListResponse<T> = ApiSuccessObjResponse<T[]>;

export interface ApiGeneralErrorResponse extends BaseApiResponse<null> {
  isSuccess: false;
  data: null;
  errors: GeneralErrorDetail;
}

export interface ApiValidationErrorResponse extends BaseApiResponse<null> {
  isSuccess: false;
  data: null;
  errors: ValidationErrorDetail[];
}

export type ApiResponse<
  T,
  E = ApiValidationErrorResponse | ApiGeneralErrorResponse
> = ApiSuccessObjResponse<T> | ApiSuccessListResponse<T> | E;
