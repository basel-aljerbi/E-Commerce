import { useState, useRef } from 'react';
import { motion } from 'framer-motion';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  User, Mail, Calendar, Shield, Phone, MapPin, Camera, Lock, Loader2, Save, Pencil,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAuthStore } from '@/store/auth';
import { useProfile, useUpdateProfile, useChangePassword, useUploadAvatar } from '@/hooks/useProfile';
import { formatDate, getImageUrl } from '@/lib/utils';
import { cn } from '@/lib/utils';
import { toast } from 'sonner';

const profileSchema = z.object({
  fullName: z.string().min(1, 'Full name is required').max(200),
  phoneNumber: z.string().max(30).nullable().optional(),
  addressLine1: z.string().max(200).nullable().optional(),
  addressLine2: z.string().max(200).nullable().optional(),
  city: z.string().max(100).nullable().optional(),
  state: z.string().max(100).nullable().optional(),
  postalCode: z.string().max(20).nullable().optional(),
  country: z.string().max(100).nullable().optional(),
});

const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Current password is required'),
  newPassword: z.string()
    .min(8, 'Password must be at least 8 characters')
    .max(100)
    .regex(/[A-Z]/, 'Must contain an uppercase letter')
    .regex(/[a-z]/, 'Must contain a lowercase letter')
    .regex(/[0-9]/, 'Must contain a number')
    .regex(/[^a-zA-Z0-9]/, 'Must contain a special character'),
});

type ProfileForm = z.infer<typeof profileSchema>;
type PasswordForm = z.infer<typeof passwordSchema>;

function InfoRow({ icon: Icon, label, value }: { icon: React.ElementType; label: string; value: string }) {
  return (
    <div className="flex items-center gap-3 p-3 rounded-lg hover:bg-muted/50 transition-colors">
      <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 shrink-0">
        <Icon className="h-5 w-5 text-primary" />
      </div>
      <div className="min-w-0">
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="font-medium truncate">{value}</p>
      </div>
    </div>
  );
}

function AvatarSection() {
  const { user } = useAuthStore();
  const uploadAvatar = useUploadAvatar();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      toast.error('Image must be less than 5MB');
      return;
    }

    if (!['image/jpeg', 'image/png', 'image/gif', 'image/webp'].includes(file.type)) {
      toast.error('Only JPEG, PNG, GIF, and WebP images are allowed');
      return;
    }

    uploadAvatar.mutate(file);
  };

  return (
    <div className="flex flex-col items-center gap-3">
      <div className="relative group">
        <div className="h-24 w-24 rounded-full overflow-hidden bg-muted ring-2 ring-border">
          {user?.avatarUrl ? (
            <img src={getImageUrl(user.avatarUrl)} alt="Avatar" className="h-full w-full object-cover" />
          ) : (
            <div className="flex h-full items-center justify-center">
              <User className="h-10 w-10 text-muted-foreground" />
            </div>
          )}
        </div>
        <button
          type="button"
          onClick={() => fileInputRef.current?.click()}
          className="absolute inset-0 flex items-center justify-center rounded-full bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
        >
          <Camera className="h-6 w-6 text-white" />
        </button>
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/png,image/gif,image/webp"
          className="hidden"
          onChange={handleFileChange}
        />
      </div>
      <p className="text-xs text-muted-foreground">Click to upload photo</p>
      {uploadAvatar.isPending && <Loader2 className="h-4 w-4 animate-spin text-primary" />}
    </div>
  );
}

export default function ProfilePage() {
  const { data, isLoading } = useProfile();
  const updateProfile = useUpdateProfile();
  const changePassword = useChangePassword();
  const { user } = useAuthStore();
  const [activeTab, setActiveTab] = useState('view');

  const profileForm = useForm<ProfileForm>({
    resolver: zodResolver(profileSchema),
    values: {
      fullName: data?.data?.fullName || user?.fullName || '',
      phoneNumber: data?.data?.phoneNumber || user?.phoneNumber || null,
      addressLine1: data?.data?.addressLine1 || user?.addressLine1 || null,
      addressLine2: data?.data?.addressLine2 || user?.addressLine2 || null,
      city: data?.data?.city || user?.city || null,
      state: data?.data?.state || user?.state || null,
      postalCode: data?.data?.postalCode || user?.postalCode || null,
      country: data?.data?.country || user?.country || null,
    },
  });

  const passwordForm = useForm<PasswordForm>({
    resolver: zodResolver(passwordSchema),
  });

  const onProfileSubmit = (formData: ProfileForm) => {
    updateProfile.mutate(formData, {
      onSuccess: () => setActiveTab('view'),
    });
  };

  const onPasswordSubmit = (formData: PasswordForm) => {
    changePassword.mutate(formData, {
      onSuccess: () => passwordForm.reset(),
    });
  };

  const profile = isLoading ? null : (data?.data || user);

  if (isLoading) {
    return (
      <div className="space-y-6 max-w-2xl mx-auto">
        <Skeleton className="h-8 w-48" />
        <Card>
          <CardContent className="p-6 space-y-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-14 rounded-lg" />
            ))}
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      className="max-w-2xl mx-auto"
    >
      <h1 className="text-3xl font-bold mb-8">My Profile</h1>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList className="mb-6">
          <TabsTrigger value="view">Profile</TabsTrigger>
          <TabsTrigger value="edit">
            <Pencil className="h-3.5 w-3.5 mr-1.5" />
            Edit
          </TabsTrigger>
          <TabsTrigger value="password">
            <Lock className="h-3.5 w-3.5 mr-1.5" />
            Password
          </TabsTrigger>
        </TabsList>

        <TabsContent value="view">
          <Card>
            <CardHeader>
              <CardTitle>Account Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              <AvatarSection />

              <div className="grid gap-3">
                <InfoRow icon={User} label="Full Name" value={profile?.fullName || 'N/A'} />
                <InfoRow icon={Mail} label="Email" value={profile?.email || 'N/A'} />
                <InfoRow icon={Phone} label="Phone" value={profile?.phoneNumber || 'N/A'} />
                <InfoRow icon={Shield} label="Role" value={profile?.role ? profile.role.charAt(0).toUpperCase() + profile.role.slice(1) : 'N/A'} />
                <InfoRow icon={Calendar} label="Member Since" value={profile?.createdAt ? formatDate(profile.createdAt) : 'N/A'} />
              </div>

              {(profile?.addressLine1 || profile?.city) && (
                <>
                  <CardTitle className="text-lg">Address</CardTitle>
                  <div className="grid gap-3">
                    {profile?.addressLine1 && <InfoRow icon={MapPin} label="Address" value={[profile.addressLine1, profile.addressLine2].filter(Boolean).join(', ')} />}
                    {profile?.city && <InfoRow icon={MapPin} label="City" value={[profile.city, profile.state].filter(Boolean).join(', ')} />}
                    {profile?.postalCode && <InfoRow icon={MapPin} label="Postal Code" value={profile.postalCode} />}
                    {profile?.country && <InfoRow icon={MapPin} label="Country" value={profile.country} />}
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="edit">
          <Card>
            <CardHeader>
              <CardTitle>Edit Profile</CardTitle>
              <CardDescription>Update your personal information</CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={profileForm.handleSubmit(onProfileSubmit)} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="fullName">Full Name</Label>
                  <Input id="fullName" {...profileForm.register('fullName')} />
                  {profileForm.formState.errors.fullName && (
                    <p className="text-sm text-destructive">{profileForm.formState.errors.fullName.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="phoneNumber">Phone Number</Label>
                  <Input id="phoneNumber" placeholder="+1 (555) 000-0000" {...profileForm.register('phoneNumber')} />
                </div>

                <div className="border-t pt-4">
                  <h3 className="font-medium mb-3">Address</h3>
                  <div className="space-y-3">
                    <div className="space-y-2">
                      <Label htmlFor="addressLine1">Address Line 1</Label>
                      <Input id="addressLine1" placeholder="123 Main St" {...profileForm.register('addressLine1')} />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="addressLine2">Address Line 2</Label>
                      <Input id="addressLine2" placeholder="Apt 4B" {...profileForm.register('addressLine2')} />
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-2">
                        <Label htmlFor="city">City</Label>
                        <Input id="city" placeholder="New York" {...profileForm.register('city')} />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="state">State</Label>
                        <Input id="state" placeholder="NY" {...profileForm.register('state')} />
                      </div>
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-2">
                        <Label htmlFor="postalCode">Postal Code</Label>
                        <Input id="postalCode" placeholder="10001" {...profileForm.register('postalCode')} />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="country">Country</Label>
                        <Input id="country" placeholder="United States" {...profileForm.register('country')} />
                      </div>
                    </div>
                  </div>
                </div>

                <div className="flex gap-3 pt-2">
                  <Button type="submit" disabled={updateProfile.isPending}>
                    {updateProfile.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-1" /> : <Save className="h-4 w-4 mr-1" />}
                    Save Changes
                  </Button>
                  <Button type="button" variant="outline" onClick={() => setActiveTab('view')}>Cancel</Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="password">
          <Card>
            <CardHeader>
              <CardTitle>Change Password</CardTitle>
              <CardDescription>Your password must be at least 8 characters with uppercase, lowercase, number, and special character</CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={passwordForm.handleSubmit(onPasswordSubmit)} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="currentPassword">Current Password</Label>
                  <Input id="currentPassword" type="password" {...passwordForm.register('currentPassword')} />
                  {passwordForm.formState.errors.currentPassword && (
                    <p className="text-sm text-destructive">{passwordForm.formState.errors.currentPassword.message}</p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="newPassword">New Password</Label>
                  <Input id="newPassword" type="password" {...passwordForm.register('newPassword')} />
                  {passwordForm.formState.errors.newPassword && (
                    <p className="text-sm text-destructive">{passwordForm.formState.errors.newPassword.message}</p>
                  )}
                </div>
                <Button type="submit" disabled={changePassword.isPending}>
                  {changePassword.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-1" /> : <Lock className="h-4 w-4 mr-1" />}
                  Change Password
                </Button>
              </form>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </motion.div>
  );
}
